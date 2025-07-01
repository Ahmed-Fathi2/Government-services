namespace Government.Contracts.Services
{
    public record ServiceResponse
     (  
        int Id ,
        string ServiceName , 
        string ServiceDescription,
        string category,
        decimal Fee,
        string ProcessingTime,
        string ContactInfo,
        bool IsAvailable


        );

}

