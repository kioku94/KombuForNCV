using Newtonsoft.Json;
using Plugin;
using System.Collections.Generic;

namespace Kombu
{
    public class KombuCommentData
    {
        public NicoLibrary.NicoLiveData.LiveCommentData Comment { get; set; }
        public UserSettingInPlugin.UserData User { get; set; }
    }

    public class KombuPlugin : IPlugin
    {
        public IPluginHost Host { get; set; }

        public string Description => "NCVからコメントを読み込んでWebSocketでホスティングするアプリケーション";
        public string Version => "1.0.0-beta";
        public string Name => "Kombu (Comment Generator)";

        public bool IsAutoRun => true;

        public void AutoRun()
        {
            var userSetting = Host.GetUserSettingInPlugin();
            var userDataMap = new Dictionary<string, UserSettingInPlugin.UserData>();
            foreach (var userData in userSetting.UserDataList) {
                userDataMap[userData.UserId] = userData;
            }

            try
            {
                var server = new KombuServer();
                var now = System.DateTime.Now;
                Host.ReceivedComment += (object sender, ReceivedCommentEventArgs eventArgs) =>
                {
                    foreach (var commentData in eventArgs.CommentDataList)
                    {
                        if (!int.TryParse(commentData.Date, out var unixTime))
                        {
                            continue;
                        }

                        var date = System.DateTimeOffset.FromUnixTimeSeconds(unixTime);
                        if (date < now)
                        {
                            continue;
                        }

                        var commentDataJson = JsonConvert.SerializeObject(new KombuCommentData {
                            Comment = commentData,
                            User = userDataMap.TryGetValue(commentData.UserId, out var userData) ? userData : null
                        });
                        server.SendMessage(commentDataJson);
                    }
                };

                server.Listen(38947);
            }
            catch (System.Exception e)
            {
                System.IO.File.AppendAllText(@"kombu-error.log", e.ToString());
            }
        }

        public void Run()
        {
        }
    }
}
