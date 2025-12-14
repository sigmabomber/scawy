using UnityEngine;
using System.Collections.Generic;
using Doody.GameEvents;
using Doody.Framework.UI;
namespace Doody.InventoryFramework.Modules
{
    public class InventoryUIModule : IInventoryModule
    {
        public string ModuleName => "Inventory UI";
        public bool IsEnabled { get; private set; } = true;

        private KeyCode toggleKey = KeyCode.Tab;
        private IInventoryFramework framework;
        private List<IInventorySystem> inventorySystems = new List<IInventorySystem>();

        public void Initialize(IInventoryFramework framework)
        {
            this.framework = framework;
        }

        public void SetToggleKey(KeyCode key)
        {
            toggleKey = key;
        }

        public void SetUnequipKey(KeyCode key)
        {
        }

        public void OnInventorySystemCreated(IInventorySystem system)
        {
            if (!inventorySystems.Contains(system))
            {
                inventorySystems.Add(system);
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
                    Events.Publish(new UIRequestToggleEvent(invSystem.inventory));


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

                }
            }
        }

        public void Shutdown()
        {
            inventorySystems.Clear();
        }
    }
}