namespace Government.Contracts.Request.Submiting
{
    public record SubmitRequestDto(

        int ServiceId,
        List<IFormFile> Files,  
        List<ServiceDataDto> ServiceData,
        string PaymentMethodId

 );
}


