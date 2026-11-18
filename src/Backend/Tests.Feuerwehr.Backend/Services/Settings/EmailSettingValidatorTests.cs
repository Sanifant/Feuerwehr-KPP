namespace de.openelp.feuerwehr.Tests.Backend.Services.Settings
{
    using de.openelp.feuerwehr.Server.Services.Settings.Validators;

    public sealed class EmailSettingValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid")]
        [InlineData("first@example.org,second@example.org")]
        [InlineData("first@example.org;second@example.org")]
        [InlineData("Name <first@example.org>")]
        [InlineData(" first@example.org")]
        [InlineData("first@example.org ")]
        [InlineData("first@example.org\r\n")]
        public void RejectsInvalidOrMultipleAddresses(string? value)
        {
            Assert.NotNull(new EmailSettingValidator().Validate(value));
        }

        [Theory]
        [InlineData("first@example.org")]
        [InlineData("first.last+test@example.org")]
        public void AcceptsSingleEmailAddresses(string value)
        {
            Assert.Null(new EmailSettingValidator().Validate(value));
        }
    }
}
