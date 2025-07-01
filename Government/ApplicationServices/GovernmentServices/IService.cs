using Government.ApplicationServices.UploadFiles;
using Government.Contracts.Request;
using Government.Contracts.Services;
using SurvayBasket.Abstractions;

namespace Government.ApplicationServices.GovernmentServices
{
    public interface IService
    {

        Task<Result<PaginationList<ServiceResponse>>> GetAllServicesAsync(ServiceQueryParameters parameters, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<ServiceResponse>>> GetAllAvailableServicesAsync(ServiceSearch serviceSearch,CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<serviceCategoryResponse>>> GetAllserviceCategoryAsync(CancellationToken cancellationToken = default);
        Task<Result<ServiceDetails>> GetServicesByIdAsync(int serviceId ,CancellationToken cancellationToken = default);
        Task<Result<ServiceResponse>> AddServiceAsync(ServiceRequest request ,CancellationToken cancellationToken = default);
        Task<Result> UpdateServiceDetailsAsync(int serviceId, ServcieDescription request ,CancellationToken cancellationToken = default);
        Task<Result> ToggleServiceAsync(int serviceId, CancellationToken cancellationToken = default);


    }
}
