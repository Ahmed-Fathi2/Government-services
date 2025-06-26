using Government.Contracts.FilesAndFileds;
using Government.Contracts.Request;
using Government.Contracts.Services;
using Mapster;
using SurvayBasket.Contracts.Authentication;
using SurvayBasket.Contracts.User.cs;

namespace Government.Mapping
{
    public class MappingConfiguration : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
      

            config.NewConfig<Service, ServiceResponse>()
             .Map(dest => dest.category, src => src.category);

            config.NewConfig<Service, ServiceDetails>()
                .Map(dest => dest.Category, src => src.category)
                .Map(dest => dest.RequiredFiles,
                     src => src.RequiredDocuments.Select(d => new FileDetails(
                         d.Id,
                         d.FileName,
                         d.ContentType,
                         d.FileExtension
                     )).ToList());


            config.NewConfig<RegisterRequest, AppUser>()
          .Map(dest => dest.UserName, src => src.Email);


            config.NewConfig<UserRequest, AppUser>()
              .Map(dest => dest.UserName, src => src.Email)
              .Map(dest => dest.EmailConfirmed, src => true);

            config.NewConfig<UserUpdate, AppUser>()
             .Map(dest => dest.UserName, src => src.Email)
             .Map(dest => dest.NormalizedUserName, src => src.Email.ToUpper());



            config.NewConfig<Request, RequestsDetails>()
           .Map(dest => dest.RequestId, src => src.Id)
           .Map(dest => dest.RequestDate, src => src.RequestDate)
           .Map(dest => dest.RequestStatus, src => src.RequestStatus)
           .Map(dest => dest.ResponseStatus, src => src.ResponseStatus)
           .Map(dest => dest.IsEditedAfterRejection, src => src.IsEditedAfterRejection)

           // Member Info
           .Map(dest => dest.MemberId, src => src.MemberId)
           .Map(dest => dest.FirstName, src => src.Member != null ? src.Member.FirstName : null)
           .Map(dest => dest.LastName, src => src.Member != null ? src.Member.LastName : null)

           // Service Info
           .Map(dest => dest.ServiceId, src => src.ServiceId)
           .Map(dest => dest.ServiceName, src => src.service != null ? src.service.ServiceName : null);


        }
    }
}
