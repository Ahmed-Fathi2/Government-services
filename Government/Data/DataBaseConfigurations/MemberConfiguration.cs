using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Government.Data.DataBaseConfigurations
{
    public class MemberConfiguration : IEntityTypeConfiguration<Member>
    {
        public void Configure(EntityTypeBuilder<Member> builder)
        {
            builder.Property(x => x.FirstName)
               .HasMaxLength(100)
               .IsRequired();

            builder.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired(false); // دي اللي بتخليه يقبل null


            builder.HasMany(x => x.Requests)
            .WithOne(x => x.Member)
            .HasForeignKey(x => x.MemberId);
        }
    }
}
