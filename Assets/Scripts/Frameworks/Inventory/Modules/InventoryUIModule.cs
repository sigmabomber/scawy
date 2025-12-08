using UnityEngine;
using System.Collections.Generic;

namespace Doody.InventoryFramework.Modules
{
    public class InventoryUIModule : IInventoryModule
    {
        public string ModuleName => "Inventory UI";
        public bool IsEnabled { get; private set; } = true;

        private KeyCode toggleKey = KeyCode.Tab;
        private KeyCode unequipKey = KeyCode.G;
        private IInventoryFramework framework;
        private List<IInventorySystem> inventorySystems = new List<IInventorySystem>();

        public void Initialize(IInventoryFramework framework)
        {
            this.framework = framework;
            Debug.Log($"[UIModule] Initialized with toggle key: {toggleKey}");
        }

        public void SetToggleKey(KeyCode key)
        {
            toggleKey = key;
        }

        public void SetUnequipKey(KeyCode key)
        {
            unequipKey = key;
        }

        public void OnInventorySystemCreated(IInventorySystem system)
        {
            if (!inventorySystems.Contains(system))
            {
                inventorySystems.Add(system);
                Debug.Log($"[UIModule] Registered inventory system: {system.SystemId}");
            }
        }

        public void Update(float deltaTime)
        {
            if (Input.GetKeyDown(toggleKey))
            {
                foreach (var system in inventorySystems)
                {
                    ToggleInventory(system);
                }
            }
        }

        private void ToggleInventory(IInventorySystem system)
        {
            if (system is InventorySystemAdapter adapter)
            {
                InventorySystem invSystem = adapter.GetInventorySystem();
                if (invSystem != null && invSystem.inventory != null)
                {
                    bool isOpen = !invSystem.inventory.activeSelf;
                    invSystem.inventory.SetActive(isOpen);

                    Time.timeScale = isOpen ? 0 : 1;

                    if (isOpen)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }

                    Debug.Log($"[UIModule] Inventory toggled: {(isOpen ? "Open" : "Closed")}");
                }
            }
        }

        public void Shutdown()
        {
            inventorySystems.Clear();
            Debug.Log($"[UIModule] Shutdown");
        }
    }
}