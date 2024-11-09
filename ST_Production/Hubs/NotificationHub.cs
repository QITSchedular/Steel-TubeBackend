namespace ST_Production.Hubs
{
    using Microsoft.AspNetCore.SignalR;
    using ST_Production.Models;
    using System;
    public class NotificationHub : Hub
    {
        private readonly NotificationService _notificationService;

        public NotificationHub(NotificationService n)
        {
            try
            {
                _notificationService = n;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in NotificationHub: {ex.Message}");
            }

        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = Context.GetHttpContext().Request.Query["userId"];
                Console.WriteLine("User Id : " + userId);
                var notifications = await _notificationService.GetNotificationsAsync(userId);
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                await Clients.Caller.SendAsync("LoadData", notifications);
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnConnectedAsync: {ex.Message}");
            }
        }
        public void NewEntryAdded(Notification_Get_Class newEntity)
        {
            try
            {
                Clients.All.SendAsync("newEntryAdded", newEntity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in NewEntryAdded: {ex.Message}");
            }
        }
    }
}
