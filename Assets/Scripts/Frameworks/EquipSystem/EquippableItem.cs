using UnityEngine;
using Doody.InventoryFramework;

public class EquippableItem : MonoBehaviour, IEquippable, IItemUsable
{
    [Header("Item Settings")]
    [SerializeField] private Vector3 equippedPosition = new Vector3(0.5f, -0.3f, 0.5f);
    [SerializeField] private Vector3 equippedRotation = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 equippedScale = Vector3.one;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Rigidbody rb;
    private Collider[] colliders;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponents<Collider>();
    }
    
    public void OnEquip(Transform equipPoint)
    {
        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;
        
        transform.SetParent(equipPoint);
        transform.localPosition = equippedPosition;
        transform.localRotation = Quaternion.Euler(equippedRotation);
        transform.localScale = equippedScale;
        
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
    }
    
    public void OnUnequip()
    {
        transform.SetParent(originalParent);
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        transform.localScale = originalScale;
        
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        foreach (var col in colliders)
        {
            col.enabled = true;
        }
    }
    
    public GameObject GetGameObject()
    {
        return gameObject;
    }
    public void OnEquip(InventorySlotsUI slot)
    {

    }
    public void OnUse(InventorySlotsUI slot)
    {

    }
    public void OnUnequip(InventorySlotsUI slot)
    {

    }

    public void OnItemStateChanged(ItemState previousState, ItemState newState)
    {

    }

  public  void OnDroppedInWorld()
    {

    }
}



