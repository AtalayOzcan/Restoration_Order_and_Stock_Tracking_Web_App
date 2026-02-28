using Microsoft.AspNetCore.SignalR;

namespace Restaurant_Order_and_Stock_Tracking_Web_App.MVC.Hubs
{
    /// <summary>
    /// Restoran genelindeki gerçek zamanlı bildirimler için SignalR Hub.
    /// Şu an desteklenen event'ler:
    ///   • WaiterCalled   – müşteri "Garson Çağır"a bastı
    ///   • WaiterDismissed – garson "İlgilenildi"ye bastı
    /// </summary>
    public class RestaurantHub : Hub
    {
        // Hub metodları intentionally boş:
        // tüm broadcast'ler server-side (controller içinden) IHubContext<RestaurantHub>
        // aracılığıyla yapılır; istemciden hub'a doğrudan çağrı gerekmez.
    }
}
