mergeInto(LibraryManager.library,
{
    RewardedAdvShowMadSlime_js: function (id)
    {
        var rewardId = UTF8ToString(id);

        if (ysdk === null || ysdk === undefined)
        {
            console.error('MadSlimeAds: Yandex SDK is not initialized');
            unityInstance.SendMessage('YandexAdsBridge', 'OnRewardedError', rewardId);
            return;
        }

        ysdk.adv.showRewardedVideo({
            callbacks: {
                onOpen: function () {
                    unityInstance.SendMessage('YandexAdsBridge', 'OnRewardedOpen', rewardId);
                },
                onRewarded: function () {
                    unityInstance.SendMessage('YandexAdsBridge', 'OnRewardedReceived', rewardId);
                },
                onClose: function () {
                    unityInstance.SendMessage('YandexAdsBridge', 'OnRewardedClosed', rewardId);
                },
                onError: function (error) {
                    console.error('MadSlimeAds: rewarded error: ' + error);
                    unityInstance.SendMessage('YandexAdsBridge', 'OnRewardedError', rewardId);
                }
            }
        });
    },

    InterstitialAdvShowMadSlime_js: function ()
    {
        if (ysdk === null || ysdk === undefined)
        {
            console.error('MadSlimeAds: Yandex SDK is not initialized');
            unityInstance.SendMessage('YandexAdsBridge', 'OnInterstitialError');
            return;
        }

        ysdk.adv.showFullscreen({
            callbacks: {
                onOpen: function () {
                    unityInstance.SendMessage('YandexAdsBridge', 'OnInterstitialOpened');
                },
                onClose: function () {
                    unityInstance.SendMessage('YandexAdsBridge', 'OnInterstitialClosed');
                },
                onError: function (error) {
                    console.error('MadSlimeAds: interstitial error: ' + error);
                    unityInstance.SendMessage('YandexAdsBridge', 'OnInterstitialError');
                }
            }
        });
    },

    SetLeaderboardScoreMadSlime_js: function (name, score, extraParam)
    {
        var leaderboardName = UTF8ToString(name);
        var extra = UTF8ToString(extraParam);

        if (ysdk === null || ysdk === undefined)
        {
            console.error('MadSlimeAds: Yandex SDK is not initialized');
            unityInstance.SendMessage('YandexAdsBridge', 'OnScoreError', leaderboardName);
            return;
        }

        ysdk.getLeaderboards().then(function (leaderboards)
        {
            leaderboards.setLeaderboardScore(leaderboardName, score, extra).then(function ()
            {
                unityInstance.SendMessage('YandexAdsBridge', 'OnScoreSet', leaderboardName);
            }).catch(function (error)
            {
                console.error('MadSlimeAds: set score error: ' + error);
                unityInstance.SendMessage('YandexAdsBridge', 'OnScoreError', leaderboardName);
            });
        }).catch(function (error)
        {
            console.error('MadSlimeAds: get leaderboards error: ' + error);
            unityInstance.SendMessage('YandexAdsBridge', 'OnScoreError', leaderboardName);
        });
    },

    GetLeaderboardEntriesMadSlime_js: function (name, quantityTop)
    {
        var leaderboardName = UTF8ToString(name);

        if (ysdk === null || ysdk === undefined)
        {
            console.error('MadSlimeAds: Yandex SDK is not initialized');
            unityInstance.SendMessage('YandexAdsBridge', 'OnEntriesError', leaderboardName);
            return;
        }

        ysdk.getLeaderboards().then(function (leaderboards)
        {
            leaderboards.getLeaderboardEntries(leaderboardName, {
                quantityTop: quantityTop,
                includeUser: true,
                quantityAround: 1
            }).then(function (result)
            {
                var entries = [];
                var rows = result.entries || [];

                for (var i = 0; i < rows.length; i++)
                {
                    var playerName = '';

                    if (rows[i].player && rows[i].player.publicName)
                    {
                        playerName = rows[i].player.publicName;
                    }

                    entries.push({
                        rank: rows[i].rank,
                        score: rows[i].score,
                        name: playerName,
                        extra: rows[i].extraParams || rows[i].extraParam || ''
                    });
                }

                var payload = JSON.stringify({
                    userRank: result.userRank || 0,
                    entries: entries
                });

                unityInstance.SendMessage('YandexAdsBridge', 'OnEntriesReceived', payload);
            }).catch(function (error)
            {
                console.error('MadSlimeAds: get entries error: ' + error);
                unityInstance.SendMessage('YandexAdsBridge', 'OnEntriesError', leaderboardName);
            });
        }).catch(function (error)
        {
            console.error('MadSlimeAds: get leaderboards error: ' + error);
            unityInstance.SendMessage('YandexAdsBridge', 'OnEntriesError', leaderboardName);
        });
    },

    GetYandexLangMadSlime_js: function ()
    {
        if (ysdk === null || ysdk === undefined || ysdk.environment === undefined || ysdk.environment.i18n === undefined)
        {
            unityInstance.SendMessage('YandexLangBridge', 'OnLangReceived', '');
            return;
        }

        var lang = ysdk.environment.i18n.lang || '';
        unityInstance.SendMessage('YandexLangBridge', 'OnLangReceived', lang);
    },

    GetYandexPlayerIdMadSlime_js: function ()
    {
        if (ysdk === null || ysdk === undefined)
        {
            unityInstance.SendMessage('YandexLangBridge', 'OnPlayerIdReceived', '');
            return;
        }

        ysdk.getPlayer({ scopes: false }).then(function (player)
        {
            var id = player.getUniqueID() || '';
            unityInstance.SendMessage('YandexLangBridge', 'OnPlayerIdReceived', id);
        }).catch(function (error)
        {
            console.error('MadSlimeAds: get player error: ' + error);
            unityInstance.SendMessage('YandexLangBridge', 'OnPlayerIdReceived', '');
        });
    }
});
