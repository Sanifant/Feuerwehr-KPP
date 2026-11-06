using System.Globalization;
using System.Net;
using De.Openelp.Feuerwehr.Server.Models.Vehicles;

namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Executes vehicle Mail Composer.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="clock">The clock.</param>
    public sealed class VehicleMailComposer(IConfiguration configuration, TimeProvider clock) : IVehicleMailComposer
    {
        /// <summary>
        /// Executes compose.
        /// </summary>
        /// <param name="reservation">The reservation.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public VehicleMailMessage Compose(VehicleReservation reservation, string recipient, bool request)
        {
            string title = request ? "Neue MTW-Reservierungsanfrage" : "MTW-Buchung bestätigt";
            string dates = $"{reservation.From.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)} bis einschlieÃŸlich {reservation.To.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}";
            string lines = $"Fahrzeug: {reservation.RadioCallSign}\nFeuerwehr: {reservation.FireDepartmentName}\n" +
                $"Anfragende Person: {reservation.RequesterName}\nZeitraum: {dates}\nBuchungsnummer: {reservation.Id}\n";
            if (reservation.Comment is not null)
            {
                lines += $"\nKommentar / Buchungsgrund:\n{reservation.Comment}\n";
            }

            string instruction = request ? "Die Anfrage ist unverbindlich. Bitte in der MTW-Ãœbersicht prüfen und bestätigen oder ablehnen." :
                "Die Buchung ist verbindlich bestätigt.";
            string link = string.Empty;
            if (Uri.TryCreate(configuration["Frontend:BaseUrl"], UriKind.Absolute, out Uri? baseUri) &&
                (baseUri.Scheme == Uri.UriSchemeHttps || baseUri.Scheme == Uri.UriSchemeHttp) && string.IsNullOrEmpty(baseUri.Query) && string.IsNullOrEmpty(baseUri.Fragment))
            {
                link = new Uri(new Uri(baseUri.AbsoluteUri.TrimEnd('/') + "/"), "vehicles").AbsoluteUri;
            }

            static string Encode(string text) => WebUtility.HtmlEncode(text);
            string html = "<!doctype html><html lang=\"de\"><body style=\"margin:0;background:#f8fafc;font-family:Arial,sans-serif;color:#1f2937\">" +
                "<table role=\"presentation\" style=\"width:100%;max-width:640px;margin:24px auto;background:#fff;border:1px solid #cbd5e1\"><tr><td style=\"padding:24px;border-top:6px solid #b91c1c\">" +
                "<img src=\"cid:feuerwehr-logo\" width=\"80\" alt=\"Feuerwehr\"><h1 style=\"font-size:24px;color:#b91c1c\">" + Encode(title) + "</h1>" +
                "<p>" + Encode(instruction) + "</p><p style=\"line-height:1.7\">" + Encode(lines).Replace("\n", "<br>") + "</p>" +
                (link.Length == 0 ? string.Empty : "<p><a style=\"color:#b91c1c\" href=\"" + Encode(link) + "\">MTW-Ãœbersicht öffnen</a></p>") +
                "</td></tr></table></body></html>";
            DateTime now = clock.GetUtcNow().UtcDateTime;
            return new VehicleMailMessage
            {
                Id = Guid.NewGuid(),
                ReservationId = reservation.Id,
                Kind = request ? "Requested" : "Confirmed",
                Recipient = recipient,
                Subject = $"{title}: {reservation.RadioCallSign}",
                TextBody = $"{title}\n\n{instruction}\n\n{lines}\n{link}",
                HtmlBody = html,
                CreatedAtUtc = now,
                NextAttemptAtUtc = now
            };
        }
    }
}
