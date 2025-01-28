using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Inventory;
using Game.Items;

public class ThrownObjectController : MonoBehaviour
{
    private bool thrown;
    public bool DopCollision = true;
    private ItemStack itemInf;

    public void SetItem(ItemStack item)
    {
        itemInf = item;
    }
    public ItemStack GetItem()
    {
        return itemInf;
    }
    private void Start()
    {
        thrown = false;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && (thrown || DopCollision == false))
        {
            FindFirstObjectByType<InventoryManager>().AddItem(itemInf);
            Destroy(gameObject);
        }
        if(collision.gameObject.CompareTag("Player"))
        {
            thrown = true;
        }
    }
}
