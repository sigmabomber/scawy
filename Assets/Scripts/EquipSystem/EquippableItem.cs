using UnityEngine;

// Interface for equippable items

public class EquippableItem : MonoBehaviour, IEquippable
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
        // Save original transform
        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;
        
        // Parent to equip point
        transform.SetParent(equipPoint);
        transform.localPosition = equippedPosition;
        transform.localRotation = Quaternion.Euler(equippedRotation);
        transform.localScale = equippedScale;
        
        // Disable physics while equipped
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Disable colliders while equipped (optional)
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
    }
    
    public void OnUnequip()
    {
        // Restore original parent and transform
        transform.SetParent(originalParent);
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        transform.localScale = originalScale;
        
        // Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        // Re-enable colliders
        foreach (var col in colliders)
        {
            col.enabled = true;
        }
    }
    
    public GameObject GetGameObject()
    {
        return gameObject;
    }
}

// Equipment manager - attach to player


