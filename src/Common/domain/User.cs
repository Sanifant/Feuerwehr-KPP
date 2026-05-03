using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace de.openelp.feuerwehr.domain
{
    public class ApplicationUser
    {
        public string Name { get; set; }

        public string Roles { get; set; }


        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

    }
}
