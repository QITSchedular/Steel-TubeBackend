using Microsoft.AspNetCore.SignalR;
using ST_Production.Controllers;
using ST_Production.Models;
using System.Data;
using System.Data.SqlClient;

namespace ST_Production.Hubs
{
    public class NotificationService
    {
        private readonly string _QIT_connection;
        public IConfiguration Configuration { get; }
       

        public NotificationService(IConfiguration configuration, IHubContext<NotificationHub> hubContext,
                                ILogger<NotificationService> logger)
        {
             
            try
            {
                Configuration = configuration;
                _QIT_connection = Configuration["connectApp:QITConnString"];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in NotificationService: {ex.Message}");
            }

        }

        static string GetHumanReadableTimeDifference(DateTime timestamp)
        {
            try
            {
                DateTime currentTime = DateTime.Now;
                TimeSpan timeDifference = currentTime - timestamp;

                if (timeDifference.TotalSeconds < 60)
                {
                    return $"{(int)timeDifference.TotalSeconds} seconds ago";
                }
                else if (timeDifference.TotalMinutes < 60)
                {
                    return $"{(int)timeDifference.TotalMinutes} minutes ago";
                }
                else if (timeDifference.TotalHours < 24)
                {
                    return $"{(int)timeDifference.TotalHours} hours ago";
                }
                else if (timeDifference.TotalDays < 30) // Approximation for a month
                {
                    return $"{(int)timeDifference.TotalDays} days ago";
                }
                else if (timeDifference.TotalDays < 365) // Approximation for a year
                {
                    int months = (int)(timeDifference.TotalDays / 30);
                    return $"{months} {(months == 1 ? "month" : "months")} ago";
                }
                else
                {
                    int years = (int)(timeDifference.TotalDays / 365);
                    return $"{years} {(years == 1 ? "year" : "years")} ago";
                }
            }
            catch (Exception ex)
            { 
                Console.WriteLine($"Error in GetHumanReadableTimeDifference: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<IEnumerable<testclass>> GetNotificationsAsync(string userName)
        {
            try
            {
                string query = $"SELECT N_Id, Notification_Text, N_Date_Time, Chk_Status FROM QIT_Notification_Master WHERE CAST(N_Date_Time AS DATE) = CAST(GETDATE() AS DATE) AND Receiver_User_Id = (select User_ID from QIT_User_Master where User_Name='{userName}')ORDER BY N_Date_Time DESC";
                int unread_Cnt = 0;
                List<Notification_Get_Class> notifications = new();
                using (SqlConnection connection = new SqlConnection(_QIT_connection))
                {
                    await connection.OpenAsync();

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                    {
                        DataTable dt = new();
                        adapter.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            notifications = dt.AsEnumerable().Select(item => new Notification_Get_Class
                            {
                                N_Id = item.Field<int>("N_Id"),
                                Notification_Text = item.Field<string>("Notification_Text"),
                                timeLimit = GetHumanReadableTimeDifference(item.Field<DateTime>("N_Date_Time")),
                                Chk_Status = item.Field<string>("Chk_Status")
                            }).ToList();
                            unread_Cnt = notifications.Where(item => item.Chk_Status == "0").Count();
                            List<testclass> data = new();
                            data.Add(new testclass { data = notifications, dataCount = unread_Cnt });

                            return data;
                        }
                    }
                }
                return Enumerable.Empty<testclass>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetNotificationsAsync : Empty Class: {ex.Message}");
                return Enumerable.Empty<testclass>();
            }
        }
    }
}
