using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace Feels.Services {
    public class InAppPurchases {
        private static StoreContext _context { get; set; }

        private static string[] _productKinds = { "Durable", "Consumable", "UnmanagedConsumable" };

        private static string[] _premiumAddonsIds = { "9N13LP56936K", "9N49HLLFDCW1", "9N47TFBCRB34"/*delete*/ };

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
            
            List<String> filterList = new List<string>(_productKinds);

            return await _context.GetAssociatedStoreProductsAsync(filterList);
        }

        public static async Task<StoreProductQueryResult> GetUserAddons() {
            InitializeContext();
            
            List<String> filterList = new List<string>(_productKinds);

            return await _context.GetUserCollectionAsync(filterList);
        }

        public static async Task<bool> DoesUserHaveAddon(string id) {
            InitializeContext();

            var foundProducts = await GetUserAddons();

            var matches = foundProducts
                        .Products
                        .Where(x => x.Value.StoreId == id);

            return matches.Count() > 0;
        }

        public static async Task<bool> DoesUserHaveAddon(string[] ids) {
            InitializeContext();

            var foundProducts = await GetUserAddons();
            
            var matches = foundProducts
                        .Products
                        .Where(x => ids.Contains(x.Value.StoreId));

            return matches.Count() > 0;
        }
        
        public static async Task<bool> IsPremiumUser() {
            InitializeContext();

            var foundProducts = await GetUserAddons();

            var matches = foundProducts
                        .Products
                        .Where(x => _premiumAddonsIds.Contains(x.Value.StoreId));

            return matches.Count() > 0;
        }

        public static async void CheckAndUpdatePremiumUser() {
            var isPremium = await IsPremiumUser();
            Settings.SavePremiumUser(isPremium);
        }
    }
}
