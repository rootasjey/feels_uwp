using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace Feels.Services {
    public class InAppPurchases {
        private static StoreContext _context = null;

        private static void InitializeContext() {
            if (_context == null) {
                _context = StoreContext.GetDefault();
            }
        }

        public static async Task<StorePurchaseResult> PurchaseAddon(string storeId) {
            InitializeContext();

            return await _context.RequestPurchaseAsync(storeId);
        }

        public static async void ConsumeAddon(string storeId) {
            if (_context == null) {
                _context = StoreContext.GetDefault();
            }

            uint quantity = 1;
            Guid trackingId = Guid.NewGuid();

            StoreConsumableResult result = await _context.ReportConsumableFulfillmentAsync(
                storeId, quantity, trackingId);

        }

        public static async Task<StoreConsumableResult> GetRemainingBalance(string storeId) {
            StoreConsumableResult result = await _context.GetConsumableBalanceRemainingAsync(storeId);
            return result;
        }

        public static async Task<StoreProductQueryResult> GetAllAddons() {
            InitializeContext();

            string[] productKinds = { "Durable", "Consumable", "UnmanagedConsumable" };
            List<String> filterList = new List<string>(productKinds);

            return await _context.GetAssociatedStoreProductsAsync(filterList);
        }

        public static async Task<StoreProductQueryResult> GetUserAddons() {
            InitializeContext();

            string[] productKinds = { "Durable", "Consumable", "UnmanagedConsumable" };
            List<String> filterList = new List<string>(productKinds);

            return await _context.GetUserCollectionAsync(filterList);
        }

        // TODO: handle array of ids
        public static async Task<bool> DoesUserHaveAddon(string id) {
            InitializeContext();

            var foundProducts = await GetUserAddons();

            var matches = foundProducts
                        .Products
                        .Where(x => x.Value.StoreId == id);

            return matches.Count() > 0;
        }
    }
}
