using UnityEngine;

namespace Doody.InventoryFramework
{
    /// <summary>
    /// Bootstrap component - attach to a GameObject in your scene to initialize the framework
    /// This will automatically create and register all modules
    /// </summary>
    public class InventoryFrameworkBootstrap : MonoBehaviour
    {
        [Header("Core Modules")]
        [SerializeField] private bool enableEquipmentModule = true;
        [SerializeField] private bool enableDragDropModule = true;
        [SerializeField] private bool enableUIModule = true;

        [Header("UI Settings")]
        [SerializeField] private KeyCode inventoryToggleKey = KeyCode.Tab;
        [SerializeField] private KeyCode unequipKey = KeyCode.G;


        private void Awake()
        {
            // Create framework if it doesn't exist
            if (InventoryFramework.Instance == null)
            {
                GameObject frameworkObj = new GameObject("InventoryFramework");
                frameworkObj.AddComponent<InventoryFramework>();
                DontDestroyOnLoad(frameworkObj);
            }

            // Register modules
            RegisterModules();
        }

        private void RegisterModules()
        {
            var framework = InventoryFramework.Instance;
            if (framework == null)
            {
                Debug.LogError("[Bootstrap] Framework not initialized!");
                return;
            }

            // Equipment Module
            if (enableEquipmentModule)
            {
                var equipmentModule = new Modules.EquipmentModule();
                framework.RegisterModule(equipmentModule);

            }

            // Drag & Drop Module
            if (enableDragDropModule)
            {
                var dragDropModule = new Modules.DragDropModule();
                framework.RegisterModule(dragDropModule);

            }

            // UI Module
            if (enableUIModule)
            {
                var uiModule = new Modules.InventoryUIModule();
                framework.RegisterModule(uiModule);

                // Configure keys
                uiModule.SetToggleKey(inventoryToggleKey);
                uiModule.SetUnequipKey(unequipKey);

            }

        }

        // Public methods to enable/disable modules at runtime
        public void EnableEquipmentModule()
        {
            if (!enableEquipmentModule)
            {
                enableEquipmentModule = true;
                var equipmentModule = new Modules.EquipmentModule();
                InventoryFramework.Instance?.RegisterModule(equipmentModule);
            }
        }

        public void DisableEquipmentModule()
        {
            if (enableEquipmentModule)
            {
                enableEquipmentModule = false;
                var module = InventoryFramework.Instance?.GetModule<Modules.EquipmentModule>();
                if (module != null)
                {
                    InventoryFramework.Instance?.UnregisterModule(module);
                }
            }
        }

        public void EnableDragDropModule()
        {
            if (!enableDragDropModule)
            {
                enableDragDropModule = true;
                var dragDropModule = new Modules.DragDropModule();
                InventoryFramework.Instance?.RegisterModule(dragDropModule);
            }
        }

        public void DisableDragDropModule()
        {
            if (enableDragDropModule)
            {
                enableDragDropModule = false;
                var module = InventoryFramework.Instance?.GetModule<Modules.DragDropModule>();
                if (module != null)
                {
                    InventoryFramework.Instance?.UnregisterModule(module);
                }
            }
        }

        public void EnableUIModule()
        {
            if (!enableUIModule)
            {
                enableUIModule = true;
                var uiModule = new Modules.InventoryUIModule();
                InventoryFramework.Instance?.RegisterModule(uiModule);
                uiModule.SetToggleKey(inventoryToggleKey);
                uiModule.SetUnequipKey(unequipKey);
            }
        }

        public void DisableUIModule()
        {
            if (enableUIModule)
            {
                enableUIModule = false;
                var module = InventoryFramework.Instance?.GetModule<Modules.InventoryUIModule>();
                if (module != null)
                {
                    InventoryFramework.Instance?.UnregisterModule(module);
                }
            }
        }

        // Reconfigure keys at runtime
        public void SetInventoryToggleKey(KeyCode key)
        {
            inventoryToggleKey = key;
            var uiModule = InventoryFramework.Instance?.GetModule<Modules.InventoryUIModule>();
            uiModule?.SetToggleKey(key);
        }

        public void SetUnequipKey(KeyCode key)
        {
            unequipKey = key;
            var uiModule = InventoryFramework.Instance?.GetModule<Modules.InventoryUIModule>();
            uiModule?.SetUnequipKey(key);
        }
    }
}