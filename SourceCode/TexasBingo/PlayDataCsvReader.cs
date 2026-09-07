using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TB.Card;
using TB.Data;
using UnityEngine;

[Serializable]
public class PlaceData
{
    public int X, Y;
    public PlayingCardType CardType;

    public PlaceData(int x, int y)
    {
        X = x; Y = y;
    }

    public PlaceData(int x, int y, PlayingCardType type)
    {
        X = x; Y = y;
        CardType = type;
    }
}

public class PlayerData
{
    private PlaceData[,] _places;
    public PlaceData[,] Places
    { 
        get 
        { 
            return _places; 
        }
        set 
        { 
            _places = value; 
        }
    }
    public int MissionId { get; set; }


    public PlayerData()
    {
        _places = new PlaceData[5,5];
        for (int y  = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                _places[x, y] = new PlaceData(x, y);
            }
        }
        MissionId = 0;
    }
}

public class PlayData
{
    public readonly int Id;
    public readonly IReadOnlyList<CardSO> Deck;
    public readonly int[] ProvMissionId;
    public readonly PlayerData[] OtherPlayers;
    public readonly GameMode Mode;

    public PlayData(int id, List<CardSO> deck, int[] provMissionId, PlayerData[] others)
    {
        Id = id;
        Deck = deck;
        ProvMissionId = provMissionId;
        OtherPlayers = others;
        Mode = GameMode.Default;
    }

    public PlayData(int id, List<CardSO> deck)
    {
        Id = id;
        Deck = deck;
        ProvMissionId = null;
        OtherPlayers = null;
        Mode = GameMode.Score;
    }
}

public static class PlayDataCsvReader
{
    private static readonly string _csvPath = Path.Combine(Application.dataPath, "03.Datas/CSV/PlayDataTable.csv");

    private static IReadOnlyDictionary<int, PlayData> _playDatas = new Dictionary<int, PlayData>();
    public static IReadOnlyDictionary<int, PlayData> PlayData
    {
        get
        {
            if (_playDatas.Count == 0)
            {
                if (!InitPlayDataWithCsv())
                    Debug.Log("비정삭적인 플레이 데이터 초기화 발견");
            }
            return _playDatas;
        } 
    }
    private struct Row
    {
        public const int Count = 23;

        public readonly string GameId;
        public readonly string CardSequence;
        public readonly string[,] Ev1Pairs; // [N,2]
        public readonly string[] Ev2;
        public readonly string[] Ev3;

        //다같이 모드 전용
        public readonly string[] Missions;
        public readonly string[] Others;

        public Row(string gmaeId, string cardSequence, string[,] ev1Pairs, string[] ev2, string[] ev3, string[] missions, string[] others)
        {
            GameId = gmaeId;
            CardSequence = cardSequence;
            Ev1Pairs = ev1Pairs;
            Ev2 = ev2;
            Ev3 = ev3;
            Missions = missions;
            Others = others;
        }
    }

    public static bool InitPlayDataWithCsv()
    {

        // BOM 포함 UTF-8도 자동 처리되게 detectEncodingFromByteOrderMarks = true
        TextAsset csvAsset = Resources.Load<TextAsset>("CSV/PlayDataTable");
        if (csvAsset == null)
        {
            Debug.LogError("PlayDataTable.csv 를 Resources/CSV 에서 찾을 수 없습니다.");
            return false;
        }

        using var sr = new StringReader(csvAsset.text);
        // 1) 헤더(설명/컬럼명) 읽고 스킵 + 인덱스 맵 구성
        string headerLine = NormalizeWhitespace(sr.ReadLine());
        
        var headers = ParseCsvLine(headerLine);

        //개수 검증
        if (headers.Count() != Row.Count)
            return false;
        ////var colIndex = BuildHeaderIndex(headers);

        // 2) 데이터 라인들에서 game_id 매칭되는 1판 찾기
        Dictionary<int, PlayData> playDatas = new();
        string line;
        while ((line = NormalizeWhitespace(sr.ReadLine())) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = ParseCsvLine(line);
            var rowData = fields.ToRowData();
            var newPlayerData = rowData.ToPlayData();
            playDatas.Add(newPlayerData.Id, newPlayerData);
        }
        _playDatas = playDatas;
        return true;
    }


    private static Row ToRowData(this List<string> dataText)
    {
        string gameId = dataText[0];
        string cardSequence = dataText[1];
        string[,] ev1Pairs = new string[,] { { dataText[2], dataText[3] }, { dataText[4], dataText[5] }, { dataText[6], dataText[7] }, { dataText[8], dataText[9] } }; // [N,2]
        string[] ev2 = new string[] { dataText[10], dataText[11], dataText[12], dataText[13] };
        string[] ev3 = new string[] { dataText[14], dataText[15], dataText[16], dataText[17] };

        string[] missions = new string[] { dataText[18], dataText[19] }; ;
        string[] others = new string[] { dataText[20], dataText[21], dataText[22] }; ;
        return new Row(gameId, cardSequence, ev1Pairs, ev2, ev3, missions, others);
    }

    private static PlayData ToPlayData(this Row rowData)
    {
        if (!int.TryParse(rowData.GameId, out int gameId))
            return null;
        List<CardSO> cards = new List<CardSO>();
        var cardTexts = rowData.CardSequence.Split(',');

        //덱 설정
        int ev1Cnt = 0;
        int ev2Cnt = 0;
        foreach (var text in cardTexts)
        {
            var card = text.ToCardData();
            cards.Add(card);
            if (card is EventCardSO eventCard)
            {
                switch (eventCard.Type)
                {
                    case TB.Card.EventCardType.Crossroads:
                        cards.Add(rowData.Ev1Pairs[ev1Cnt, 0].ToCardData());
                        cards.Add(rowData.Ev1Pairs[ev1Cnt, 1].ToCardData());
                        ev1Cnt++;
                        break;
                    case TB.Card.EventCardType.Tunnel:
                        cards.Add(rowData.Ev2[ev2Cnt].ToCardData());
                        ev2Cnt++;
                        break;
                    case TB.Card.EventCardType.SandstormRT:
                        cards.Add(rowData.Ev3[0].ToCardData());
                        break;
                    case TB.Card.EventCardType.SandstormLT:
                        cards.Add(rowData.Ev3[1].ToCardData());
                        break;
                    case TB.Card.EventCardType.SandstormRD:
                        cards.Add(rowData.Ev3[2].ToCardData());
                        break;
                    case TB.Card.EventCardType.SandstormLD:
                        cards.Add(rowData.Ev3[3].ToCardData());
                        break;
                    default:
                        break;
                }
            }
        }
        
        //미션&AI 데이터 미사용 시 반환 - 싱글 모드
        if (rowData.Missions.All(s => string.IsNullOrEmpty(s)) && rowData.Others.All(s => string.IsNullOrEmpty(s)))
        {
            return new PlayData(gameId, cards);
        }
        
        //미션 카드 설정
        List<int> missionIndexs = new();
        foreach(string missiontext in rowData.Missions)
        {
            if (!int.TryParse(missiontext.Replace("MS", ""), out int missionIndex))
                return null;
            missionIndexs.Add(missionIndex);
        }

        //플레이어 데이터 설정
        List<PlayerData> playerDatas = new List<PlayerData>();
        foreach (string otherText in rowData.Others)
        {
            playerDatas.Add(otherText.ToPlayerData());
        }


        return new PlayData(gameId,cards,missionIndexs.ToArray(),playerDatas.ToArray());
    }

    private static PlayerData ToPlayerData(this string dataText)
    {
        string[] splitDatas = SplitJsonObjectsWithoutBraces(dataText.Trim('[', ']')).ToArray();
        string[] placeDatas = splitDatas[0].Split(",");
        string[] missionData = splitDatas[1].Split(":").Select(s => s.Trim('\"')).ToArray();
        
        PlayerData playerData = new PlayerData();
        foreach (string placeData in placeDatas)
        {
            var card = placeData.ToPlaceData(out int x, out int y);
            playerData.Places[x,y].CardType = card.Type;
        }
        var missionIndexText = missionData[1].Replace("MS", "");
        if (!int.TryParse(missionIndexText, out int missionIndex))
        {
            Debug.Log("문제1");
            return null;
        }
        playerData.MissionId = missionIndex;
        return playerData;
    }

    /// <summary>
    /// CSV 한 줄을 필드 리스트로 파싱.
    /// - 따옴표로 감싼 필드 지원
    /// - 내부 따옴표는 "" 로 이스케이프 된 것을 " 로 복원
    /// - 콤마는 따옴표 밖에서만 구분자로 처리
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        if (line == null)
            return result;

        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // "" -> " (이스케이프된 따옴표)
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++; // 다음 따옴표 하나 더 소비
                    }
                    else
                    {
                        inQuotes = false; // 따옴표 종료
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"')
                {
                    inQuotes = true; // 따옴표 시작
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        result.Add(sb.ToString());
        return result;
    }

    public static List<string> SplitJsonObjectsWithoutBraces(string s)
    {
        var result = new List<string>();

        int depth = 0;
        int start = -1;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];

            if (c == '{')
            {
                if (depth == 0)
                    start = i + 1; // '{' 다음부터 시작

                depth++;
            }
            else if (c == '}')
            {
                depth--;

                if (depth == 0 && start >= 0)
                {
                    // '}' 직전까지
                    result.Add(s.Substring(start, i - start));
                    start = -1;
                }
            }
        }

        return result;
    }
    private static string NormalizeWhitespace(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        return s
            .Replace('\u00A0', ' ')  // NBSP
            .Replace('\t', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace(" ", "");
    }
}
