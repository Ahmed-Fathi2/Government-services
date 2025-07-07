using Government.Contracts.Dashboard;
using Government.Contracts.DashBoard;
using Government.Contracts.Services;
using Microsoft.EntityFrameworkCore;

namespace Government.ApplicationServices.Results
{
    public class DashboardService(AppDbContext context) : IDashboardService
    {
        private readonly AppDbContext _context = context;

        public async Task<Result<Overview>> GetOverviewAsync()
        {
            // num of users
            var TotalUsers = await _context.Requests
                                  .Select(x => x.MemberId)
                                  .Distinct()
                                  .CountAsync();


            var TotalAvailableServices = await _context.Services
                                     .Where(s => s.IsAvailable)
                                     .CountAsync();

            var ApprovedRequests = await _context.Requests
                                     .Where(r => r.ResponseStatus == "Approve" || r.ResponseStatus == "Responded")
                                     .CountAsync();

            var RejectedRequests = await _context.Requests
                                     .Where(r => r.ResponseStatus == "Reject")
                                     .CountAsync();

            var PendingRequests = await _context.Requests
                                     .Where(r => r.ResponseStatus == "No Response" || r.ResponseStatus == "None")
                                     .CountAsync();

     ;

            var result = new Overview(TotalUsers, TotalAvailableServices, ApprovedRequests, RejectedRequests, PendingRequests);

            return Result.Success(result);

        }

        public async Task<Result<RequestStatisticsDto>> GetRequestStatisticsAsync()
        {

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);


            int newRequests = await _context.Requests
                 .Where(r => r.RequestDate.Year == startOfMonth.Year && r.RequestDate.Month == startOfMonth.Month)
                 .CountAsync();



            int completedRequests = await _context.Requests
                .Where(r => r.RequestStatus == "Completed")
                .CountAsync();


            int rejectedRequests = await _context.Requests
                .Where(r => r.RequestStatus == "Rejected")
                .CountAsync();


            int pendingRequests = await _context.Requests
                .Where(r => r.RequestStatus == "Pending")
                .CountAsync();



            var userNames = await _context.Members
                    .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                    .ToListAsync();

            var topRequestUsers = await _context.Requests
                .GroupBy(r => r.MemberId)
                .Select(g => new {
                    MemberId = g.Key,
                    RequestCount = g.Count()
                })
                .OrderByDescending(g => g.RequestCount)
                .Take(5)
                .ToListAsync();

            var topUsersWithNames = topRequestUsers
                .Select(u => {
                    var userName = userNames.FirstOrDefault(x => x.Id == u.MemberId)?.FullName ?? "Unknown";
                    return new UserUsageDto(u.MemberId, userName, u.RequestCount);
                })
                .ToList();



            var data = new RequestStatisticsDto(newRequests, completedRequests, rejectedRequests, pendingRequests, topUsersWithNames);

            return Result.Success(data);
        }

        public async Task<Result<ServiceStatisticsDto>> GetServiceStatisticsAsync()
        {



            var totalServices = await _context.Services.CountAsync();


            var serviceNames = await _context.Services
                .Select(s => new { s.Id, s.ServiceName })
                .ToListAsync();

            var requestedServicesRaw = await _context.Requests
                .GroupBy(r => r.ServiceId)
                .Select(g => new
                {
                    ServiceId = g.Key,
                    RequestCount = g.Count()
                })
                .OrderByDescending(g => g.RequestCount)
                .ToListAsync();


            var requestedServices = requestedServicesRaw
                .Select(r =>
                {
                    var name = serviceNames.FirstOrDefault(s => s.Id == r.ServiceId)?.ServiceName ?? "Unknown";
                    return new ServiceUsageDto(r.ServiceId, name, r.RequestCount);
                })
                .ToList();

            var mostRequestedServices = requestedServices.Take(3).ToList();
            var leastRequestedServices = requestedServices.TakeLast(3).ToList();

            var data = new ServiceStatisticsDto(totalServices, mostRequestedServices, leastRequestedServices);

            return Result.Success(data);

        }
        public async Task<Result<IEnumerable<MostRequested>>> GetMostRequestedServicesAsync()
        {

            //var mostRequestedServices = await _context.Requests
            //                      .GroupBy(r => r.ServiceId)
            //                      .Select(x => new
            //                      {
            //                          ServiceId = x.Key,
            //                          RequestCount = x.Count()
            //                      })
            //                      .OrderByDescending(u => u.RequestCount)
            //                      .Take(5)
            //                      .ToListAsync();


            //var serviceIds = mostRequestedServices.Select(x => x.ServiceId).ToList();


            //var services = await _context.Services
            //.Where(s => serviceIds.Contains(s.Id))
            //.Include(s => s.RequiredDocuments)
            //.Select(s => new MostRequested
            //(
            //    s.Id,
            //    s.ServiceName,
            //    s.ServiceDescription,
            //    s.category,
            //    s.Fee,
            //    s.ProcessingTime,
            //    s.RequiredDocuments.Select(d => d.FileName).ToList()
            //))
            //.ToListAsync();

            //return Result.Success<IEnumerable<MostRequested>>(services);
            var services = await _context.Services
                            .Where(s => s.IsAvailable)                 
                            .OrderByDescending(s => s.Requests.Count) 
                            .Take(5)                             
                            .Select(s => new MostRequested(
                                s.Id,
                                s.ServiceName,
                                s.ServiceDescription,
                                s.category,
                                s.Fee,
                                s.ProcessingTime,
                                s.RequiredDocuments.Select(d => d.FileName).ToList()
                            ))
                            .ToListAsync();

            return Result.Success<IEnumerable<MostRequested>>(services);

        }

        public async Task<Result<IEnumerable<MonthlyCountDto>>> GetRequestStatisticsPerMonthAsync()
        {
            var currentYear = DateTime.UtcNow.Year;

            var monthlyCounts = await _context.Requests
                .Where(r => r.RequestDate.Year == currentYear)
                .GroupBy(r => r.RequestDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();


            var fullMonthlyStats = Enumerable.Range(1, 12)
                .Select(m => new MonthlyCountDto(
                    Month: m,
                    Count: monthlyCounts.FirstOrDefault(x => x.Month == m)?.Count ?? 0
                ))
                .ToList();

            return Result.Success<IEnumerable<MonthlyCountDto>>(fullMonthlyStats);

        }
    }
}
