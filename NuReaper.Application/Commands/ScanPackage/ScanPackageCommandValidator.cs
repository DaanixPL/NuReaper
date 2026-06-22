using FluentValidation;

namespace NuReaper.Application.Commands.ScanPackage
{
    public class ScanPackageCommandValidator : AbstractValidator<ScanPackageCommand>
    {
        public ScanPackageCommandValidator()
        {
            RuleFor(x => x.url)
                .NotEmpty()
                .WithMessage("Source is required.")
                .Must(BeNugetUrlOrLocalNupkg)
                .WithMessage("Source must be a nuget.org package URL or a local .nupkg path/file URI.");
        }

        private static bool BeNugetUrlOrLocalNupkg(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return false;

            if (source.Contains("nuget.org/packages/", StringComparison.OrdinalIgnoreCase)
                || source.Contains("nuget.org/api/v2/package/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (source.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                && source.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return source.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase);
        }
    }
}