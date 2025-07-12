using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Government.Data.DataBaseConfigurations
{
    public class AdminImageConfiguration : IEntityTypeConfiguration<AdminImage>
    {
        public void Configure(EntityTypeBuilder<AdminImage> builder)
        {
            builder.Property(x => x.ImageName)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.ContentType)
                .HasMaxLength(100);


            builder.Property(x => x.ImageExtension).HasMaxLength(10);

            builder.ToTable("AdminImages");
        }
    }
}
