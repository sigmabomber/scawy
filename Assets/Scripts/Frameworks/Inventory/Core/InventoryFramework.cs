using UnityEngine;
using System.Collections.Generic;
using Doody.GameEvents;

namespace Doody.InventoryFramework
{
    /// <summary>
    /// Main framework implementation - Singleton manager for all inventory systems
    /// </summary>
    public class InventoryFramework : MonoBehaviour, IInventoryFramework
    {
        public static InventoryFramework Instance { get; private set; }

        private Dictionary<System.Type, IInventoryModule> modules = new Dictionary<System.Type, IInventoryModule>();
        private Dictionary<string, IInventorySystem> inventorySystems = new Dictionary<string, IInventorySystem>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (var module in modules.Values)
            {
                if (module.IsEnabled)
                {
                    module.Update(deltaTime);
                }
            }
        }

        public void RegisterModule(IInventoryModule module)
        {
            var moduleType = module.GetType();
            if (modules.ContainsKey(moduleType))
            {
                Debug.LogWarning($"[Framework] Module {module.ModuleName} already registered!");
                return;
            }

            modules[moduleType] = module;
            module.Initialize(this);

            foreach (var system in inventorySystems.Values)
            {
                module.OnInventorySystemCreated(system);
            }

        }

        public void UnregisterModule(IInventoryModule module)
        {
            var moduleType = module.GetType();
            if (modules.ContainsKey(moduleType))
            {
                module.Shutdown();
                modules.Remove(moduleType);
                Debug.Log($"[Framework] Unregistered module: {module.ModuleName}");
            }
        }

        public T GetModule<T>() where T : IInventoryModule
        {
            if (modules.TryGetValue(typeof(T), out var module))
            {
                return (T)module;
            }
            return default(T);
        }

        public bool HasModule<T>() where T : IInventoryModule
        {
            return modules.ContainsKey(typeof(T));
        }

        public IInventorySystem GetInventorySystem(string systemId)
        {
            inventorySystems.TryGetValue(systemId, out var system);
            return system;
        }

        public void RegisterInventorySystem(IInventorySystem system)
        {
            if (inventorySystems.ContainsKey(system.SystemId))
            {
                Debug.LogWarning($"[Framework] Inventory system {system.SystemId} already registered!");
                return;
            }

            inventorySystems[system.SystemId] = system;

            foreach (var module in modules.Values)
            {
                if (module.IsEnabled)
                {
                    module.OnInventorySystemCreated(system);
                }
            }


            Events.Publish(new InventorySystemRegistered(system.SystemId));
        }

        private void OnDestroy()
        {
            foreach (var module in modules.Values)
            {
                module.Shutdown();
            }
            modules.Clear();
            inventorySystems.Clear();
        }
    }
}