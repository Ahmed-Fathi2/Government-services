using Government.Contracts.FilesAndFileds;
using Government.Errors;
using Microsoft.EntityFrameworkCore;

namespace Government.ApplicationServices.Fields
{
    public class FieldServcie(AppDbContext context):IFieldServcie
    {
        private readonly AppDbContext _context = context;

        public async Task<Result<IEnumerable<UpdateFields>>> GetUserRequestFileds(int RequestId, CancellationToken cancellationToken)
        {

            var request = await _context.Requests.FindAsync(RequestId);
            if (request == null)
                return Result.Falire<IEnumerable<UpdateFields>>(RequestErrors.RequestNotFound);

            var Userdata = await _context.ServicesData.Where(x => x.RequestId == RequestId).
                                                        Select(x => new UpdateFields(
                                                            x.FieldId,
                                                            x.Field.FieldName,
                                                            x.Field.HtmlType,
                                                            x.Id,
                                                            x.FieldValueString,
                                                            x.FieldValueInt,
                                                            x.FieldValueFloat,
                                                            x.FieldValueDate,
                                                            x.FieldValueString != null ? "string" :
                                                            x.FieldValueInt != null ? "int" :
                                                            x.FieldValueFloat != null ? "float" :
                                                            x.FieldValueDate != null ? "date" : "unknown"

                                                            ))
                                                        .AsNoTracking()
                                                        .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<UpdateFields>>(Userdata);
        }

        public async Task<Result> UpdateUserFieldsAsync(int RequestId, UserFields userFields, CancellationToken cancellationToken = default)
        {
            var request = await _context.Requests.FindAsync(RequestId, cancellationToken);
            if (request == null)
                return Result.Falire(RequestErrors.RequestNotFound);

            foreach (var userData in userFields.ServiceData)
            {
                var existingData = await _context.ServicesData
                    .FirstOrDefaultAsync(s => s.RequestId == RequestId && s.FieldId == userData.FieldId);

                if (existingData != null)
                {

                    if (existingData.FieldValueString != userData.FieldValueString)
                        existingData.FieldValueString = userData.FieldValueString;

                    if (existingData.FieldValueInt != userData.FieldValueInt)
                        existingData.FieldValueInt = userData.FieldValueInt;

                    if (existingData.FieldValueFloat != userData.FieldValueFloat)
                        existingData.FieldValueFloat = userData.FieldValueFloat;

                    if (existingData.FieldValueDate != userData.FieldValueDate)
                        existingData.FieldValueDate = userData.FieldValueDate;

                    _context.ServicesData.Update(existingData);
                }
                else
                {

                    var newServiceData = new ServiceData
                    {
                        RequestId = RequestId,
                        FieldId = userData.FieldId,
                        FieldValueString = userData.FieldValueString,
                        FieldValueInt = userData.FieldValueInt,
                        FieldValueFloat = userData.FieldValueFloat,
                        FieldValueDate = userData.FieldValueDate
                    };

                    await _context.ServicesData.AddAsync(newServiceData);
                }
            }

            if (request.RequestStatus == "Rejected")
            {
                request.IsEditedAfterRejection = true;
                request.RequestStatus = "Edited" ;
                request.ResponseStatus = "No New Response" ;

            }

            await _context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<IEnumerable<FieldsResponse>>> GetServiceFieldAsync(int serviceId, CancellationToken cancellationToken)
        {
            var service = await context.Services.SingleOrDefaultAsync(x => x.Id == serviceId, cancellationToken); // check service id 

            if (service is null)
                return Result.Falire<IEnumerable<FieldsResponse>>(ServiceError.ServiceNotFound);

            var fields = await context.ServicesField
                             .Where(x => x.ServiceId == serviceId)
                             .Select(x => new FieldsResponse(
                                 x.Field.Id,
                                 x.Field.FieldName,
                                 x.Field.Description,
                                 x.Field.HtmlType
                                 //x.Field.HtmlType == "text" || x.Field.HtmlType == "textarea" ? "string" :
                                 //x.Field.HtmlType == "number" ? "int" : // افتراض إن number بيبدأ كـ int، الـ frontend هيحدد float لو فيه كسور
                                 //x.Field.HtmlType == "date" || x.Field.HtmlType == "datetime-local" ? "date" : "unknown"
                                 ))
                             .AsNoTracking()
                             .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<FieldsResponse>>(fields);


        }

        public async Task<Result> UpdateFieldsAsync(int ServcieId, FieldsTest fieldsTest, CancellationToken cancellationToken = default)
        {

            var service = await _context.Services.FindAsync(ServcieId);
            if (service == null)
                return Result.Falire(ServiceError.ServiceNotFound);


            var existingFields = await _context.Fields.ToListAsync();

            var fieldEntities = new List<Field>();

            foreach (var field in fieldsTest.ServiceFields)
            {
                var existing = existingFields.FirstOrDefault(f => f.FieldName == field.FieldName);
                if (existing != null)
                {
                    fieldEntities.Add(existing);
                }
                else
                {

                    var newField = new Field
                    {
                        FieldName = field.FieldName,
                        Description = field.Description,
                        HtmlType = field.HtmlType,
                    };
                    _context.Fields.Add(newField);
                    fieldEntities.Add(newField);
                }
            }

            await _context.SaveChangesAsync();


            var oldLinks = _context.ServicesField.Where(sf => sf.ServiceId == ServcieId);
            _context.ServicesField.RemoveRange(oldLinks);
            await _context.SaveChangesAsync();

            foreach (var field in fieldEntities)
            {
                _context.ServicesField.Add(new ServiceField
                {
                    ServiceId = ServcieId,
                    FieldId = field.Id
                });
            }

            await _context.SaveChangesAsync();

            return Result.Success();
        }

    }
}
