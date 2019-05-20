using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace QnABot.traduzione
{
    public  class Traduzione
    {
        string to = "";
        string testo = "";
        string from = "";
        string trad = "";
        public static Translate traduzione;
        public async Task <string> fromIT(string _testo, string _from, string _to)
        {
             to = _to;
             testo = _testo;
             from = _from;
             trad = "";
            switch (to)
            {
                case "EN":
                    await Traduci(testo, from, to);
                    trad = traduzione.data.translation;
                    break;

                case "DE":
                    to = "EN";
                    await Traduci(testo, from, to);
                    testo = traduzione.data.translation;
                    from = "EN";
                    to = "DE";
                    await Traduci(testo, from, to);
                    trad = traduzione.data.translation;
                    break;

                case "ES":
                    to = "EN";
                    await Traduci(testo, from, to);
                    testo = traduzione.data.translation;
                    from = "EN";
                    to = "ES";
                    await Traduci(testo, from, to);
                    trad = traduzione.data.translation;
                    break;
            }

            return trad;
        }

        public async Task <string>   toIT(string _testo, string _from, string _to)
        {
             to = _to;
             testo = _testo;
             from = _from;
             trad = "";
            switch (from)
            {
                case "EN":

                    await Traduci(testo, from, to);
                    trad = traduzione.data.translation;
                    break;

                case "DE":
                    from = "DE";
                    to = "EN";
                    await Traduci(testo, from, to);
                    testo = traduzione.data.translation;
                    from = "EN";
                    to = "IT";
                    await Traduci(testo, from, to);
                    trad = traduzione.data.translation;
                    break;

                case "ES":
                    from = "ES";
                    to = "EN";
                    await Traduci(testo, from, to); 
                    testo = traduzione.data.translation;
                    from = "EN";
                    to = "IT";
                    await Traduci(testo, from, to);
                    trad = traduzione.data.translation;
                    break;
            }

            return trad;
        }

        public static async Task Traduci(string query, string from, string to)
        {
            string url = $"http://hackabot.modernmt.eu/translate?source={from.ToLower()}&target={to.ToLower()}&q={query}";
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            using (HttpClient client = new HttpClient(httpClientHandler, true))
            {
                HttpResponseMessage response = null;
                client.DefaultRequestHeaders.Add("MMT-ApiKey", "615A6DD4-B0A3-47E4-9E85-9EE01629DA74");
                response = await client.GetAsync(url).ConfigureAwait(false);

                string stringResponse = await response.Content.ReadAsStringAsync();


                 traduzione = JsonConvert.DeserializeObject<Translate>(stringResponse);


            }
        }
    }
   
}
