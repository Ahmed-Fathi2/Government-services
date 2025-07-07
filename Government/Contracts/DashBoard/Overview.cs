namespace Government.Contracts.Dashboard
{
    public record Overview
    (

         int TotalUsers ,
         int TotalServices ,
         int ApprovedRequests ,
         int RejectedRequests ,
         int PendingRequests

    );
}
