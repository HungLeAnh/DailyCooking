using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
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

// Gem packs are credited to the host's restaurant save, so only the host can buy, and a
// purchase is granted (and saved) before it is confirmed with the store. Anything that can't
// be granted yet stays pending and is redelivered by the store later.
public class IAPManager : MonoBehaviour
{
    private const string GRANTED_TRANSACTIONS_PREFS_KEY = "IAP_GrantedTransactions";
    private const int MAX_REMEMBERED_TRANSACTIONS = 50;
    private const string GOOGLE_PLAY_TANGLE_TYPE = "UnityEngine.Purchasing.Security.GooglePlayTangle";

    private static readonly Dictionary<string, int> GemsByProductId = new Dictionary<string, int>
    {
        { ProductIDs.GEMPACK1, 50 },
        { ProductIDs.GEMPACK2, 100 },
        { ProductIDs.GEMPACK3, 200 },
    };

    public static IAPManager Instance;
    public event Action OnProductsReady;

    private StoreController storeController;
    private bool isStoreConnected;
    private CrossPlatformValidator googlePlayValidator;
    private bool isValidatorResolved;

    public bool AreProductsReady { get; private set; }

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
        if (Instance != this) return;
        storeController = UnityIAPServices.StoreController();

        // Subscribe before Connect: pending purchases from a previous session may arrive at once.
        storeController.OnStoreDisconnected += OnStoreDisconnected;
        storeController.OnProductsFetched += OnProductsFetched;
        storeController.OnProductsFetchFailed += OnProductsFetchFailed;
        storeController.OnPurchasesFetched += OnPurchasesFetched;
        storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
        storeController.OnPurchasePending += OnPurchasePending;
        storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        storeController.OnPurchaseFailed += OnPurchaseFailed;
        storeController.OnPurchaseDeferred += OnPurchaseDeferred;

        try
        {
            // IAP 5.1 has no OnStoreConnected event; the store is connected once Connect completes.
            await storeController.Connect();
            OnStoreConnected();
        }
        catch (Exception e)
        {
            Debug.LogError($"IAP: store connection failed: {e.Message}");
        }
    }

    private void OnStoreConnected()
    {
        isStoreConnected = true;
        storeController.FetchProducts(new List<ProductDefinition>
        {
            new(ProductIDs.GEMPACK1, ProductType.Consumable),
            new(ProductIDs.GEMPACK2, ProductType.Consumable),
            new(ProductIDs.GEMPACK3, ProductType.Consumable)
        });
        // Redelivers purchases left pending (e.g. the app closed before they were granted).
        storeController.FetchPurchases();
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
    {
        isStoreConnected = false;
        Debug.LogWarning($"IAP: store disconnected: {failure.Message}");
    }

    private void OnProductsFetched(List<Product> products)
    {
        AreProductsReady = true;
        OnProductsReady?.Invoke();
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning($"IAP: product fetch failed: {failure.FailureReason}");
    }

    private void OnPurchasesFetched(Orders orders)
    {
        Debug.Log($"IAP: {orders.PendingOrders.Count} pending purchase(s) to process.");
    }

    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        Debug.LogWarning($"IAP: purchase fetch failed: {failure.Message}");
    }

    // Called by GameManager once a hosted restaurant is running, to pick up purchases that
    // could not be granted earlier.
    public void RetryPendingPurchases()
    {
        if (isStoreConnected)
            storeController.FetchPurchases();
    }

    private void OnPurchasePending(PendingOrder order)
    {
        string productId = order.CartOrdered.Items().FirstOrDefault()?.Product?.definition.id;
        if (productId == null || !GemsByProductId.TryGetValue(productId, out int gems))
        {
            Debug.LogWarning($"IAP: unknown product in pending order: {productId}");
            return;
        }
        if (!CanGrantNow())
        {
            // Left unconfirmed: the store keeps it and redelivers it (RetryPendingPurchases).
            Debug.Log($"IAP: {productId} stays pending until a hosted restaurant is loaded.");
            return;
        }
        if (!IsReceiptValid(order))
        {
            // Not granted and not confirmed; Google refunds unconfirmed purchases automatically.
            Debug.LogError($"IAP: receipt validation failed for {productId}; not granting.");
            return;
        }

        string transactionId = order.Info.TransactionID;
        if (!IsAlreadyGranted(transactionId))
        {
            GameManager.Instance.ServerAddGems(gems);
            GameManager.Instance.SaveGameImmediate();
            RememberGranted(transactionId);
        }
        storeController.ConfirmPurchase(order);
    }

    private void OnPurchaseConfirmed(Order order)
    {
        switch (order)
        {
            case ConfirmedOrder confirmedOrder:
                Debug.Log($"IAP: confirmed {confirmedOrder.CartOrdered.Items().FirstOrDefault()?.Product?.definition.id}");
                break;
            case FailedOrder failedOrder:
                Debug.LogError($"IAP: confirmation failed: {failedOrder.FailureReason} - {failedOrder.Details}");
                break;
        }
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        var product = order.CartOrdered.Items().FirstOrDefault()?.Product;
        Debug.Log($"IAP: purchase failed for {product?.definition.id}, reason: {order.FailureReason} - {order.Details}");
    }

    private void OnPurchaseDeferred(DeferredOrder order)
    {
        // Granted later through OnPurchasePending once the payment completes.
        Debug.Log("IAP: purchase deferred, waiting for the payment to complete.");
    }

    private static bool CanGrantNow()
    {
        return GameManager.Instance != null && GameManager.Instance.IsServer && GameManager.Instance.GameData != null;
    }

    public bool CanPurchase()
    {
        return isStoreConnected && AreProductsReady && CanGrantNow();
    }

    public void BuyProduct(ProductKeys key)
    {
        // Gems belong to the restaurant save, which only the host owns.
        if (!CanGrantNow())
        {
            UIManager.Instance.ShowAlertMessage("Only the restaurant owner can buy gems.");
            return;
        }
        if (!isStoreConnected || !AreProductsReady)
        {
            UIManager.Instance.ShowAlertMessage("The store is not available right now.");
            return;
        }
        storeController.PurchaseProduct(GetProductId(key));
    }

    // Null until the products are fetched (see OnProductsReady).
    public Product GetProductMetaData(ProductKeys key)
    {
        if (storeController == null || !AreProductsReady)
            return null;
        return storeController.GetProductById(GetProductId(key));
    }

    private static string GetProductId(ProductKeys key)
    {
        return key switch
        {
            ProductKeys.gempack1 => ProductIDs.GEMPACK1,
            ProductKeys.gempack2 => ProductIDs.GEMPACK2,
            ProductKeys.gempack3 => ProductIDs.GEMPACK3,
            _ => null
        };
    }

    #region Receipt validation

    // Validates Google Play receipts locally with the obfuscated key generated by
    // Services > In-App Purchasing > Receipt Validation Obfuscator (GooglePlayTangle). The
    // generated class is looked up at runtime so the project builds before it exists.
    private bool IsReceiptValid(PendingOrder order)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CrossPlatformValidator validator = GetGooglePlayValidator();
        if (validator == null)
        {
            Debug.LogError("IAP: GooglePlayTangle is missing, receipts are NOT validated. Run the Receipt Validation Obfuscator.");
            return true;
        }
        try
        {
            validator.Validate(order.Info.Receipt);
            return true;
        }
        catch (IAPSecurityException e)
        {
            Debug.LogError($"IAP: invalid receipt: {e.Message}");
            return false;
        }
#else
        return true;
#endif
    }

    private CrossPlatformValidator GetGooglePlayValidator()
    {
        if (isValidatorResolved)
            return googlePlayValidator;
        isValidatorResolved = true;

        Type tangleType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(GOOGLE_PLAY_TANGLE_TYPE, false))
            .FirstOrDefault(type => type != null);
        byte[] googlePublicKey = tangleType?.GetMethod("Data")?.Invoke(null, null) as byte[];
        if (googlePublicKey != null && googlePublicKey.Length > 0)
            googlePlayValidator = new CrossPlatformValidator(googlePublicKey, Application.identifier);
        return googlePlayValidator;
    }

    #endregion

    #region Granted transaction memory

    // A purchase can be redelivered if the app closed after granting but before confirming;
    // remember granted transactions on this device so it is never credited twice.
    private static bool IsAlreadyGranted(string transactionId)
    {
        return !string.IsNullOrEmpty(transactionId) && LoadGrantedTransactions().Contains(transactionId);
    }

    private static void RememberGranted(string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId)) return;
        List<string> granted = LoadGrantedTransactions();
        granted.Add(transactionId);
        if (granted.Count > MAX_REMEMBERED_TRANSACTIONS)
            granted.RemoveRange(0, granted.Count - MAX_REMEMBERED_TRANSACTIONS);
        PlayerPrefs.SetString(GRANTED_TRANSACTIONS_PREFS_KEY, string.Join("\n", granted));
        PlayerPrefs.Save();
    }

    private static List<string> LoadGrantedTransactions()
    {
        string stored = PlayerPrefs.GetString(GRANTED_TRANSACTIONS_PREFS_KEY, string.Empty);
        return string.IsNullOrEmpty(stored)
            ? new List<string>()
            : stored.Split('\n').Where(id => !string.IsNullOrEmpty(id)).ToList();
    }

    #endregion
}
