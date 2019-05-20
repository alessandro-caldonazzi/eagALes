// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.IO;
using CsvHelper;
using QnABot.traduzione;
using Microsoft.AspNetCore.Hosting;
using QnABot.data;
using System.Linq;
using QnABot;
using System.Text.RegularExpressions;

namespace Microsoft.BotBuilderSamples
{

    public class QnABot : ActivityHandler
    {
        private BotState _userState;
        string Btn_menu_1 = "Cerca Lavoro";
        string Btn_menu_2 = "Trova Operatore";
        string Btn_menu_3 = "Iscriviti Al Centro Impiego";
        string Btn_menu_4 = "Curriculum Vitae";

        string Btn_Cen_in_1 = "Carta di identità";
        string Btn_Cen_in_2 = "Codice fiscale";
        string Btn_Cen_in_3 = "Curriculum Vitae";
        string Btn_Cen_in_4 = "NO";
        IStatePropertyAccessor<UserProfile> userStateAccessors;
        private BotState _conversationState;
        private readonly IConfiguration _configuration;
        private readonly ILogger<QnABot> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHostingEnvironment _hostingEnvironment;

        public QnABot(IConfiguration configuration, UserState userState,  IHostingEnvironment hostingEnvironment, ILogger<QnABot> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _hostingEnvironment = hostingEnvironment;
            _userState = userState;
        }

        // QNA
        
        public async Task<string> Qna(ITurnContext turnContext)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var qnaMaker = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = _configuration["QnAKnowledgebaseId"],
                EndpointKey = _configuration["QnAAuthKey"],
                Host = _configuration["QnAEndpointHostName"]
            },
            null,
            httpClient);

            _logger.LogInformation("Calling QnA Maker");

            // The actual call to the QnA Maker service. 
            var response = await qnaMaker.GetAnswersAsync(turnContext);
            
            if (response != null && response.Length > 0)
            {
                

                if (response[0].Answer.Contains("$#")){
                    var baloons = response[0].Answer.Split("$#");
                    //int limit = 0;
                    
                    if (response[0].Answer.Contains("$SP"))
                    {
                        
                        var stringa = response[0].Answer.Replace("$SP", "");
                        var str_contr = stringa.Split("$#");
                        for (int i = 0; i < baloons.Length; i++)
                        {
                            await turnContext.SendActivityAsync(MessageFactory.Text(await Traduci(str_contr[i], turnContext, true)));
                        }

                        return "null";

                    }
                    else
                    {
                        
                        var stringa = response[0].Answer.Replace("$SP", "");
                        var str_contr = stringa.Split("$#");
                        for (int i = 0; i < baloons.Length - 1; i++)
                        {
                            str_contr[i] = str_contr[i].Replace("$AB", "");
                            await turnContext.SendActivityAsync(MessageFactory.Text(await Traduci(str_contr[i], turnContext, true)));
                        }
                        if(response[0].Answer.Contains("$SN"))
                        {
                            return str_contr[baloons.Length - 1];
                        }
                        else if(response[0].Answer.Contains("$AB"))
                        {
                            
                            await turnContext.SendActivityAsync(await Traduci(str_contr[baloons.Length - 1], turnContext, true));
                            return "null";
                        }
                        else
                        {
                            return str_contr[baloons.Length - 1];
                        }
                        
                    }
                   
                }
                else
                {
                    if ( response[0].Answer.Contains("$SN"))
                    {
                        var  Sn = response[0].Answer.Replace("$SN", "");
                        //await turnContext.SendActivityAsync(MessageFactory.Text(Sn[0]));
                        return Sn;
                    }
                    else
                    {
                        if (response[0].Answer.Contains("$SP"))
                        {
                            var Sp = response[0].Answer.Split("$SP");
                            await turnContext.SendActivityAsync(MessageFactory.Text(await Traduci(Sp[1], turnContext, true)));

                            return "null";

                        }
                        else
                        {
                            await turnContext.SendActivityAsync(MessageFactory.Text(await Traduci(response[0].Answer, turnContext, true)));
                            return "null";
                        }
                        
                    }
                    
                }


            }
            else
            {

                await turnContext.SendActivityAsync(MessageFactory.Text(await Traduci("Non trovo la risposta alla domanda.", turnContext, true)));
                return "null";
            }
        }
        
        public void Dati(ITurnContext turnContext, string nome)
        {
            using (var reader = new StreamReader(_hostingEnvironment.ContentRootPath + "/data/data_lavori.csv"))
            {
                using (var csv = new CsvReader(reader))
                {
                    var records = csv.GetRecords<Offerta>();
                    List<Offerta> offerte = records.ToList();

                    List<Offerta> off_it = offerte.Where(o => o.SEDE.ToLowerInvariant().Contains(nome.ToLower())).ToList();

                    turnContext.SendActivityAsync("Ho trovato questo in: " + off_it.FirstOrDefault().SEDE + ". Sono richieste le lingue: " + off_it.FirstOrDefault().LINGUA + "E' richiesto un " + off_it.FirstOrDefault().IMPIEGO + " Tipo di contratto: " + off_it.FirstOrDefault().CONTRATTO);
                    

                }
            }
            
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var userStateAccessors = _userState.CreateProperty<UserProfile>(nameof(UserProfile));

            string mex = turnContext.Activity.Text;
            string benvenuto = "Hi, I'm Eaglaes  and I'm your Telegram virtual assistant of the 'Agenzia del lavoro - Provincia Autonoma di Trento'. I will do my best to give all the information you need. ";
             bool isNumber = int.TryParse(mex, out int eta);
            if ( isNumber == true){
                mex = "escape";
                if (eta_contr(eta))
                {
                    turnContext.Activity.Text = "$eta36";
                    await Qna(turnContext);
                    await turnContext.SendActivityAsync(await Traduci("Che ne dici se faccio una piccola ricerca per te? Scrivi il nome di uno stato in cui vuoi trovare lavoro presso un progetto EURES.", turnContext, true));


                }
                else if (eta < 18)
                {

                    turnContext.Activity.Text = "$eta0";
                    await Qna(turnContext);
                }
                else
                {
                    turnContext.Activity.Text = "$eta18";
                    await Qna(turnContext);
                    await turnContext.SendActivityAsync(await Traduci("Che ne dici se faccio una piccola ricerca per te? Scrivi il nome di uno stato in cui vuoi trovare lavoro presso un progetto EURES.", turnContext, true));

                }

            }
            
            var loc= turnContext.Activity.Entities?.Where(t => t.Type == "Place").Select(t => t.GetAs<Place>()).FirstOrDefault();
            if (loc != null)
            {
                turnContext.Activity.Text = "Trova Operatore";
                await Qna(turnContext);
                return;
            }
            
            switch (mex)
            {
                
                case "escape":
                    break;
                case "/start":

                    await turnContext.SendActivityAsync(MessageFactory.Text(await Traduci(benvenuto, turnContext, true)));
                    var ben = new HeroCard
                    {
                        Text = "But first, can you tell me the language you can speak?",
                        Buttons = new List<CardAction> { new CardAction(ActionTypes.PostBack, "Italiano", value: "IT"),
                            new CardAction(ActionTypes.PostBack, "English", value: "EN"),
                            new CardAction(ActionTypes.PostBack, "Deutsh", value: "DE"),
                            new CardAction(ActionTypes.PostBack, "Espanol", value: "ES")

                        },
                    };
                    var reply = ((Activity)turnContext.Activity).CreateReply();
                    reply.Attachments.Add(ben.ToAttachment());
                    await turnContext.SendActivityAsync(reply);


                    break;

                /* case "CARD":

                     var herocard = new HeroCard
                     {
                         Title = "titolo",
                         Text = "Testo",
                         Buttons = new List<CardAction> { new CardAction(ActionTypes.PostBack, "TESTO BUT", value: "payload") },
                     };
                     var reply = ((Activity)turnContext.Activity).CreateReply();
                     reply.Attachments.Add(herocard.ToAttachment());
                     await turnContext.SendActivityAsync(reply);

                     break;
                 */
                case var someVal when new Regex("(Ital|Norve|Germ|Island|Belg|Fran|Berl|Austr)",RegexOptions.IgnoreCase).IsMatch(someVal):
                    Dati(turnContext,turnContext.Activity.Text);
                    break;
                case "dati":
                    //Dati();
                    break;

                case "$cartaidentita":
                    var r1 = await Qna(turnContext);
                    await Cont_In(turnContext, r1);
                    break;
                
                case "$codicefiscale":
                    var r2 = await Qna(turnContext);
                    await Cont_In(turnContext, r2);
                    break;

                case "$cv":
                    var r3 = await Qna(turnContext);
                    await Cont_In(turnContext, r3);
                    break;

                case "$n_2":
                    var r4 = await Qna(turnContext);
                    
                    break;


                case "$lingua":
                    var userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile());
                    await turnContext.SendActivityAsync(await Traduci(userProfile.Lingua, turnContext, true));
                    break;
                
                case "EN":
                case "IT":
                case "ES":
                case "DE":
                    userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile());
                    userProfile.Lingua = turnContext.Activity.Text;
                    await _userState.SaveChangesAsync(turnContext, false, cancellationToken);

                    turnContext.Activity.Text = "$IT";

                    var ret=await Qna(turnContext);
                   
                    await Menu(turnContext, ret);
                    break;

                case var someVal when new Regex("Cerca Lavoro").IsMatch(someVal):
                
                    var result= await Qna(turnContext);
                    await Scelta(turnContext, result);
                    break;
                case var someVal when new Regex("Iscriviti Al Centro Impiego").IsMatch(someVal):
                
                    var result1 = await Qna(turnContext);
                    await Cen_in(turnContext, result1);
                    break;
                case "menu":
                case "/menu":
                case "Menu":
                case "/Menu":

                    await Menu(turnContext, "Menu");
                    break;
                    
                case var someVal when new Regex("Trova Operatore").IsMatch(someVal):
                   // string str_op = Traduci("Che ne dici di condividermi la tua posizione?", turnContext, true).ToString();
                    await  turnContext.SendActivityAsync(await Traduci("Che ne dici di condividermi la tua posizione?", turnContext, true));

                    break;

                default:

                   var ret_qna = await Qna(turnContext);
                    if (ret_qna != "null")
                    {
                        await Scelta(turnContext, ret_qna);
                    }
                    break;
            }

            
        }

        public async Task Cont_In(ITurnContext turnContext, string testo)
        {
            if (testo == "null")
            {
                testo = "Scegli:";
            }
            if (turnContext.Activity.Text == "$cartaidentita")
            {
                string mex = turnContext.Activity.Text;
                JObject msg = JObject.FromObject(new
	            {
		            method = "sendMessage",
		            parameters = new
		            {
			            text = testo,
			            parse_mode = "HTML",
			            reply_markup = new
			            {
				            inline_keyboard = new[]
				            { 
                                new[] 
                                {			   
				                    new
				                    {
					                    text = await Traduci(Btn_Cen_in_2, turnContext, true),
					                    callback_data = "$codicefiscale",
				                    },
				                    new
				                    {
					                    text = await Traduci(Btn_Cen_in_3, turnContext, true),
					                    callback_data = "cv",
				                    }
				                },
			            
                                new[] 
                                {			   
				                    new
				                    {
					                    text = await Traduci(Btn_Cen_in_4, turnContext, true),
					                    callback_data = "n_2",
				                    }
				                }	
			                },
				            resize_keyboard = true,
			            },
		            },
	            });
                var reply = ((Activity)turnContext.Activity).CreateReply();
                reply.ChannelData = msg;
                await turnContext.SendActivityAsync(reply);
            
            } else if(turnContext.Activity.Text == "$codicefiscale") 
            {
                string mex = turnContext.Activity.Text;
                JObject msg = JObject.FromObject(new
	            {
		            method = "sendMessage",
		            parameters = new
		            {
			            text = testo,
			            parse_mode = "HTML",
			            reply_markup = new
			            {
				            inline_keyboard = new[]
				            { 
                                new[] 
                                {			   
				                    new
				                    {
					                    text = await Traduci(Btn_Cen_in_1, turnContext, true),
					                    callback_data = "$cartaidentita",
				                    },
				                    new
				                    {
					                    text = await Traduci(Btn_Cen_in_3, turnContext, true),
					                    callback_data = "cv",
				                    }
				                },
			            
                                new[] 
                                {			   
				                    new
				                    {
					                    text = await Traduci(Btn_Cen_in_4, turnContext, true),
					                    callback_data = "n_2",
				                    }
				                }	
			                },
				            resize_keyboard = true,
			            },
		            },
	            });
                var reply = ((Activity)turnContext.Activity).CreateReply();
                reply.ChannelData = msg;
                await turnContext.SendActivityAsync(reply);

            } else if(turnContext.Activity.Text == "$cv")
            {
                string mex = turnContext.Activity.Text;
                 JObject msg = JObject.FromObject(new
	            {
		            method = "sendMessage",
		            parameters = new
		            {
			            text = testo,
			            parse_mode = "HTML",
			            reply_markup = new
			            {
				            inline_keyboard = new[]
				            { 
                                new[] 
                                {			   
				                    new
				                    {
					                    text = await Traduci(Btn_Cen_in_1, turnContext, true),
					                    callback_data = "$cartaidentita",
				                    },
				                    new
				                    {
					                    text = await Traduci(Btn_Cen_in_2, turnContext, true),
					                    callback_data = "$codicefiscale",
				                    }
				                },
			            
                                new[] 
                                {			   
				                    new
				                    {
					                    text = await Traduci(Btn_Cen_in_4, turnContext, true),
					                    callback_data = "n_2",
				                    }
				                }	
			                },
				            resize_keyboard = true,
			            },
		            },
	            });
                var reply = ((Activity)turnContext.Activity).CreateReply();
                reply.ChannelData = msg;
                await turnContext.SendActivityAsync(reply);
            }
        }

        public async Task Cen_in(ITurnContext turnContext, string testo)
        {
            if (testo == "null")
            {
                testo = "Scegli: ";
            }
           string mex = turnContext.Activity.Text;
           var men = new HeroCard
           {
               Text = testo,
               Buttons = new List<CardAction>
               {
                   new CardAction(ActionTypes.PostBack, await Traduci(Btn_Cen_in_1, turnContext, true), value: "$cartaidentita"),
                   new CardAction(ActionTypes.PostBack, await Traduci(Btn_Cen_in_2, turnContext, true), value: "$codicefiscale"),
                   new CardAction(ActionTypes.PostBack, await Traduci(Btn_Cen_in_3, turnContext, true), value: "$cv"),
                   new CardAction(ActionTypes.PostBack, await Traduci(Btn_Cen_in_4, turnContext, true), value: "$n_2")

               },
           };
           var reply = ((Activity)turnContext.Activity).CreateReply();
           reply.Attachments.Add(men.ToAttachment());
          // reply.ChannelData = msg;
           await turnContext.SendActivityAsync(reply);
       }

       public async Task<string> Traduci(string stringa, ITurnContext turnContext, bool direct)
       {
            var userStateAccessors = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile());
            string str= "";
            string emoj = "";
            if (stringa.Contains("\\"))
            {
                int index = stringa.IndexOf("\\");
                emoj = stringa.Substring(index, index + 9);
            str = stringa.Remove(index);
            }

            if (userProfile.Lingua == "IT")
            {
                str= stringa;
            }
            else if(direct)
            {
                
                switch (userProfile.Lingua)
                {
                    case "EN":

                        str= await T_Trans(stringa, "IT", "EN");
                        break;
                    case "ES":
                        str=await T_Trans(stringa, "IT", "ES");
                        break;
                    case "DE":
                        str= await T_Trans(stringa, "IT", "DE");
                        break;
                    
                }
            }
            else
            {
                switch (userProfile.Lingua)
                {
                    case "EN":

                       str= await T_Trans(stringa, "EN", "IT");
                        break;
                    case "ES":
                        str= await T_Trans(stringa, "ES", "IT");
                        break;
                    case "DE":
                        str=await T_Trans(stringa, "DE", "IT");
                        break;

                }
            }
            //return await T_Trans(stringa, "it", "it");
            return str;
       }
       public async Task Scelta(ITurnContext turnContext, string testo)
       {
            if (testo == "null")
            {
                testo = "Scegli:";
            }
           var men = new HeroCard
           {
               Text = testo,
               Buttons = new List<CardAction>
               {
                   new CardAction(ActionTypes.PostBack, await Traduci("Si \U0001F44D", turnContext, true), value: "$s_1"),
                   new CardAction(ActionTypes.PostBack, await Traduci("No \U0001F44E", turnContext, true), value: "$n_1"),


               },
           };
           var reply = ((Activity)turnContext.Activity).CreateReply();
           reply.Attachments.Add(men.ToAttachment());
           // reply.ChannelData = msg;
           await turnContext.SendActivityAsync(reply);
       }
       public bool eta_contr(int eta)
       {
           if(eta>0&&eta<36){

               return false;

           }else {

               return true;

           }

       }
       public async Task Menu(ITurnContext turnContext, string testo)
       {
           /*
           string mex = turnContext.Activity.Text;
           var men = new HeroCard
           {
               Text = testo,
               Buttons = new List<CardAction>
               {
                   new CardAction(ActionTypes.PostBack, Traduci(Btn_menu_1), value: "$cerca"),
                   new CardAction(ActionTypes.PostBack, Traduci(Btn_menu_2), value: "$trova_operatore"),
                   new CardAction(ActionTypes.PostBack, Traduci(Btn_menu_3), value: "$iscriviti"),
                   new CardAction(ActionTypes.PostBack, Traduci(Btn_menu_4), value: "$cv")

               },
           };
           var reply = ((Activity)turnContext.Activity).CreateReply();
           reply.Attachments.Add(men.ToAttachment());
          // reply.ChannelData = msg;
           await turnContext.SendActivityAsync(reply);

           */
            var reply = turnContext.Activity.CreateReply(await Traduci(testo, turnContext, true));
            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction() { Title = await Traduci(Btn_menu_1, turnContext, true), Type = ActionTypes.ImBack},
                    new CardAction() { Title = await Traduci(Btn_menu_2, turnContext, true), Type = ActionTypes.ImBack},
                    new CardAction() { Title = await Traduci(Btn_menu_3, turnContext, true), Type = ActionTypes.ImBack},
                    new CardAction() { Title = await Traduci(Btn_menu_4, turnContext, true), Type = ActionTypes.ImBack},
                },
            };
            await turnContext.SendActivityAsync(reply);
        }
     
        public async static Task <string> T_Trans(string testo, string from, string to)
        {
            Traduzione traduzione = new Traduzione();

            if (from == "IT")
            {
                
                return await traduzione.fromIT(testo, from, to);
            }
            else
            {
                return await traduzione.toIT(testo, from, to);
            }
        }
    }
}
