using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
public static class ProductIDs
{
    public const string GEMPACK1 = "mr.gempack1";
    public const string GEMPACK2 = "mr.gempack2";
    public const string GEMPACK3 = "mr.gempack3";
}
[Serializable]
public enum ProductKeys
{
    gempack1 = 0,
    gempack2 = 1, 
    gempack3 = 2,
}
public class IAPManager : MonoBehaviour
{
    
    private StoreController storeController;
    
    public static IAPManager Instance;

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

    private async void Start()
    {
        storeController = UnityIAPServices.StoreController();

        // Listen to store events
        storeController.OnPurchasePending += OnPurchasePending;
        storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        storeController.OnPurchaseFailed += OnPurchaseFailed;

        await storeController.Connect();

        // Fetch your products
        FetchProducts();
    }

    private void FetchProducts()
    {
        var products = new List<ProductDefinition>
        {
            new(ProductIDs.GEMPACK1, ProductType.Consumable),
            new(ProductIDs.GEMPACK2, ProductType.Consumable),
            new(ProductIDs.GEMPACK3, ProductType.Consumable)
        };

        storeController.FetchProducts(products);
    }
    private void OnPurchasePending(PendingOrder order)
    {
        var product = order.CartOrdered.Items().First()?.Product;
        Debug.Log($"Pending purchase: {product.definition.id}");

        // Grant reward now if you want immediate effect
        // But for consumables, best practice is to wait until confirmed

        // Confirm purchase so the transaction is completed
        storeController.ConfirmPurchase(order);
    }

    private void OnPurchaseConfirmed(Order order)
    {
        var product = order.CartOrdered.Items().First()?.Product;
        Debug.Log($"Confirmed purchase: {product.definition.id}");

        GrantReward(product.definition.id);
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        var product = order.CartOrdered.Items().First()?.Product;
        Debug.Log($"Purchase failed for {product?.definition.id}, reason: {order.FailureReason}");
    }
    private void GrantReward(string productId)
    {
        switch (productId)
        {
            case ProductIDs.GEMPACK1:
                GameManager.Instance.UpdateRestaurantGemsServerRpc(50);
                break;

            case ProductIDs.GEMPACK2:
                GameManager.Instance.UpdateRestaurantGemsServerRpc(100);
                break;

            case ProductIDs.GEMPACK3:
                GameManager.Instance.UpdateRestaurantGemsServerRpc(200);
                break;

            default:
                Debug.LogWarning("Unknown product: " + productId);
                break;
        }
    }
    public void BuyProduct(ProductKeys key)
    {
        string productId = key switch
        {
            ProductKeys.gempack1=> ProductIDs.GEMPACK1,
            ProductKeys.gempack2 => ProductIDs.GEMPACK2,
            ProductKeys.gempack3 => ProductIDs.GEMPACK3,
            _ => null
        };

        if (!string.IsNullOrEmpty(productId))
        {
            storeController.PurchaseProduct(productId);
        }
        else
        {
            Debug.LogWarning("Invalid product key: " + key);
        }
    }

    public Product GetProductMetaData(ProductKeys key)
    {
        string productId = key switch
        {
            ProductKeys.gempack1 => ProductIDs.GEMPACK1,
            ProductKeys.gempack2 => ProductIDs.GEMPACK2,
            ProductKeys.gempack3 => ProductIDs.GEMPACK3,
            _ => null
        };
        return storeController.GetProductById(productId);
    }
}