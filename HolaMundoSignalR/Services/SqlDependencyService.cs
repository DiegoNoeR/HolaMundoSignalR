using HolaMundoSignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HolaMundoSignalR.Services
{
    public class SqlDependencyService : IDatabaseChangeNotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly IHubContext<ChatHub> _chatHub;

        public SqlDependencyService(IConfiguration configuration, IHubContext<ChatHub> chatHub)
        {
            _configuration = configuration;
            _chatHub = chatHub;
        }
        public void config()
        {
            SuscribirseALosCambiosDeLaTablaUsers();
        }

        private void SuscribirseALosCambiosDeLaTablaUsers()
        {
            string connString = _configuration.GetConnectionString("DefaultConnection");

            using (var conn = new SqlConnection(connString))
            {
                conn.Open();

                using (var cmd = new SqlCommand(@"SELECT Name FROM [dbo].Users", conn))
                {
                    cmd.Notification = null;
                    SqlDependency dependency = new SqlDependency(cmd);
                    dependency.OnChange += Dependency_OnChange;
                    SqlDependency.Start(connString);
                    cmd.ExecuteReader();    
                }
            }
        }

        private void Dependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                string mensaje = ObtenerMensajeAMostrar(e);
                _chatHub.Clients.All.SendAsync("ReceiveMessage", "Admin", mensaje);
                SuscribirseALosCambiosDeLaTablaUsers();
            } 
        }

        private string ObtenerMensajeAMostrar(SqlNotificationEventArgs e)
        {
            switch (e.Info)
            {
                case SqlNotificationInfo.Insert:
                    return "Un registro ha sido insertado";
                case SqlNotificationInfo.Delete:
                    return "Un registro ha sido borrado";
                case SqlNotificationInfo.Update:
                    return "Un registro ha sido actualizado";
                default:
                    return "Un cambio desconocido ha ocurrido";
            }
        }
    }
}
