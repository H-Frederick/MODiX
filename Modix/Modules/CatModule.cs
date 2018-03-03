namespace Modix.Modules
{
    using Discord.Commands;
    using Services.Cat;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public enum Media
    {
        Picture, // 0
        Gif // 1
    }

    public class CatModule : ModuleBase
    {
        private static Media _mediaType;
        private readonly ICatService _catService;

        public CatModule(ICatService catService)
        {
            _catService = catService;
        }

        [Command("cat"), Summary("Gets a cat")]
        public async Task Cat(string param = "")
        {
            var message = string.Empty;

            // It can take a Media type parameter however the command has to be !cat picture or !cat gif all of the time. 
            // If !cat is used, it says too few parameters passed and fails. 
            // I want to retain !cat functionality. 
            // Attempt at making an optional parameter where cat commands can have an optional parameter to have content
            // in relation to the parameter. Proposed new param is: !cat [param1] [param2]
            // param1 = something you want your cat picture to come with. '!cat wet' would return an image of a cat
            //          tagged with wet
            // param2 = null -> just return an img, gif -> return a gif
            // Never mind, I'm gonna need to change services to use thecatapi.com if I want these changes to be used
            if (string.IsNullOrWhiteSpace(param))
            {
                _mediaType = Media.Picture;
            }
            else if (string.Equals("gif", param, StringComparison.OrdinalIgnoreCase))
            {
                _mediaType = Media.Gif;
            }
            else
            {
                await Context.Channel.SendMessageAsync("Use `!cat` or `!cat gif`");
                return;
            }

            using (var cts = new CancellationTokenSource(5000))
            {
                var token = cts.Token;

                message = await _catService.HandleCat(_mediaType, token);
            }

            // Send the link
            await Context.Channel.SendMessageAsync(message);
        }
    }
}