using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HubstaffReport.Core.Helpers;
using HubstaffReport.Core.Models;
using HubstaffReport.Core.Services;
using InvoiceMaker.Model;

namespace InvoiceMaker.Helpers
{
    public class HubstaffApi
    {
        private static readonly HubstaffApi _instance = new HubstaffApi();

        public static HubstaffApi Instance => _instance;

        private readonly ApiService _api = new ApiService(new FileService(), new RequestService());

        private readonly Dictionary<string, Project[]> _userProjects = new Dictionary<string, Project[]>();

        private HubstaffApi() { }

        public async Task<TimeSpan> GetWorkedHours(BillableUnit unit, int projectId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var user = unit.User;
                if (user == null) return TimeSpan.Zero;

                Tokens.AuthToken = user.AuthToken;
                if (!_userProjects.ContainsKey(unit.User.Name))
                {
                    var projects = await _api.GetProjectsForUser(user);
                    _userProjects[unit.User.Name] = projects.ToArray();
                }
                long totalSeconds = 0;
                var project = _userProjects[unit.User.Name].FirstOrDefault(p => p.Id == projectId);
                if (project != null)
                {
                    var report = await _api.GetReportForUser(user, project, startDate, endDate);
                    totalSeconds = report.TotalWorkHours; 
                }

                return TimeSpan.FromSeconds(totalSeconds);
            }
            catch (Exception exc)
            {
                //TODO: Log errors
                return TimeSpan.Zero;
            }
        }

        public List<User> GetUsers()
        {
            try
            {
                return _api.GetUsers().ToList();
            }
            catch (Exception exc)
            {
                return new List<User>();
            }
        }
    }
}