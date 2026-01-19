using Doody.Framework.UI;
using Doody.GameEvents;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

            foreach (var system in inventorySystems)
            {
                ToggleInventory(system);

              
            }
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
            // Check for keyboard input
            if (Input.GetKeyDown(toggleKey))
            {
                foreach (var system in inventorySystems)
                {
                    ToggleInventory(system);
                }
            }

            // Check for controller input (Start or Select button)
            if (Gamepad.current != null)
            {
                if (Gamepad.current.dpad.up.wasPressedThisFrame )
                {
                    foreach (var system in inventorySystems)
                    {
                        ToggleInventory(system);
                    }
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

                    // Publish the toggle event
                    Events.Publish(new UIRequestToggleEvent(invSystem.inventory));

                    // Handle cursor state
                    if (isOpen)
                    {
                        // Open inventory
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;

                        // Setup controller navigation
                        if (InventoryNavigation.Instance != null)
                        {
                            InventoryNavigation.Instance.SetInventoryOpen(true, invSystem.inventory);
                        }
                    }
                    else
                    {
                        if (InventoryNavigation.Instance != null)
                        {
                            InventoryNavigation.Instance.SetInventoryOpen(false);
                        }

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