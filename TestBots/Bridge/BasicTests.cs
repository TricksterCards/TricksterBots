namespace TestBots
{
    public static class BasicTests
    {
        //  adapted from trickster's unit/bridge/tests.json
        public static readonly BasicTest[] Tests =
        {
            //  Openings
            new BasicTest
            {
                history = null,
                hand = "2C3C4C 2D3D4D 2H3H4H 2S3S4S5S", // 0 HCP, 0 total
                bid = "Pass",
                type = "Opening"
            },
            new BasicTest
            {
                history = null,
                hand = "AC3C4C AD3D4D AH3H4H 2S3S4S5S", // 12 HCP, 12 total
                bid = "Pass",
                type = "Opening"
            },
            new BasicTest
            {
                history = null,
                hand = "ACJC4C AD3D4D AHJH4H 2S3S4S5S", // 13 HCP, 13 total (3C, 3D, 3H, 4S)
                bid = "1♣",
                type = "Opening"
            },
            new BasicTest
            {
                history = new[] { "Pass" },
                hand = "ACJC4C5C AD3D4D5D AH3H4H 2S3S", // 13 HCP, 13 total (4C, 4D, 3H, 2S)
                bid = "1♦",
                type = "Opening"
            },
            new BasicTest
            {
                history = new[] { "Pass", "Pass" },
                hand = "AC3C4C AD3D4D AH3H4H5H6H 2S3S", // 12 HCP, 13 total (3C, 3D, 5H, 2S)
                bid = "1♥",
                type = "Opening"
            },
            new BasicTest
            {
                history = new[] { "Pass", "Pass", "Pass" },
                hand = "AC3C4C AD3D4D AH3H 2S3S4S5S6S", // 12 HCP, 13 total (3C, 3D, 2H, 5S)
                bid = "1♠",
                type = "Opening"
            },
            new BasicTest
            {
                history = new[] { "Pass", "Pass", "Pass" },
                hand = "AC3C4C KD3D4D KH3H 2S3S4S5S6S", // 10 HCP, 11 total, 15 HCP + Spades (3C, 3D, 2H, 5S) - Rule of 15
                bid = "1♠",
                type = "Opening"
            },
            new BasicTest
            {
                history = new[] { "Pass", "Pass", "Pass" },
                hand = "KC3C4C KD3D4D KH3H 2S3S4S5S6S", // 9 HCP, 10 total, 14 HCP + Spades (3C, 3D, 2H, 5S) - NOT Rule of 15
                bid = "Pass",
                type = "Opening"
            },
            new BasicTest
            {
                history = new[] { "Pass", "Pass", "Pass" },
                hand = "KC6C ADKDQDTD3D AHTH4H3H 9S3S", // 16 HCP, 17 total (2C, 5D, 4H, 2S)
                bid = "1♦",
                type = "Opening"
            },
            new BasicTest
            {
                history = null,
                hand = "KCJCTC KDJDTD KHJHTH KSJSTS9S", // 16 HCP, 16 total
                bid = "1NT",
                type = "Opening"
            },
            new BasicTest
            {
                history = null,
                hand = "ACKCQC ADKDQD AHKHQH ASKSQSJS", // 37 HCP, 37 total
                bid = "2♣",
                type = "Opening (Strong)"
            },

            //  Responses

            new BasicTest
            {
                history = new[] { "1NT", "Pass" },
                hand = "TC ADKDTD9D TH9H8H7H ASKSTS9S", // 14 HCP, 14 total
                bid = "2♣",
                type = "Response (Stayman)"
            },

            new BasicTest
            {
                history = new[] { "1♠", "2♣" },
                hand = "ACTC9C8C7C6C AD TH9H7H KSJSTS", // 12 HCP, 15 total (dummy)
                bid = "3♣",
                type = "Response (Cuebid)"
            },

            new BasicTest
            {
                history = new[] { "1♦", "Pass" },
                hand = "5C4C2C JD8D5D3D QHJH8H ASJS6S", // 9 HCP, 9 total
                bid = "2♦", //  Assume 4+ Diamonds from 1D opening per SAYC Booklet
                type = "Response"
            },

            //  Overcalls

            new BasicTest
            {
                history = new[] { "1♦" },
                hand = "AC3C4C JD3D4D AHKH4H5H6H 2S3S", // 12 HCP, 13 total (3C, 3D, 5H, 2S)
                bid = "1♥",
                type = "Overcall"
            },
            new BasicTest
            {
                history = new[] { "1♥" },
                hand = "ACKC5C ADKD5D KHQH5H JS9S8S7S", // 19 HCP, 19 total (3C, 3D, 3H, 4S)
                bid = "X",
                type = "Overcall (Takeout Double)"
            },
            new BasicTest
            {
                history = new[] { "1♦" },
                hand = "ASQS7S6S5S AHJH9H6H5H TD9D 6C", // 11 HCP
                bid = "2♦",
                type = "Overcall (Michaels)"
            },
            new BasicTest
            {
                history = new[] { "4♦" },
                hand = "ASQS7S6S5S AHJH9H6H5H TD9D 6C", // 11 HCP
                bid = "Pass", // May be better options, but currently avoiding Michaels above the game level
                type = "Overcall"
            },

            //  Advances

            new BasicTest
            {
                history = new[] { "1♦", "1♥", "Pass" },
                hand = "AC3C4C AD3D4D5D 3H4H5H 2S3S4S", // 8 HCP, 9 total (3C, 4D, 3H, 4S)
                bid = "2♥",
                type = "Advance"
            },
            new BasicTest
            {
                history = new[] { "1♣", "1♥", "Pass" },
                hand = "AC3C4C AD3D4D AH3H4H5H6H 2S3S", // 12 HCP, 13 total (3C, 3D, 5H, 2S)
                bid = "2♣",
                type = "Advance (Cuebid)"
            },
            new BasicTest
            {
                history = new[] { "1♦", "1♥", "Pass" },
                hand = "AC3C4C AD3D4D AH3H4H5H6H 2S3S", // 12 HCP, 13 total (3C, 3D, 5H, 2S)
                bid = "2♦",
                type = "Advance (Cuebid)"
            },
            new BasicTest
            {
                history = new[] { "1♥", "X", "Pass" },
                hand = "TC9C8C TD9D8D TH9H8H TS9S8S7S6S", // 0 HCP, 0 total (3C, 3D, 3H, 5S)
                bid = "1♠",
                type = "Advance Takeout Double"
            },

            //  Opener Rebid

            new BasicTest
            {
                history = new[] { "1NT", "Pass", "2♣", "Pass" },
                hand = "TC9C ADKDTD9D TH9H8H7H ASKSTS", // 14 HCP, 14 total
                bid = "2♥",
                type = "Opener Rebid (Answer Stayman)"
            },

            //  Responder Rebid

            new BasicTest
            {
                history = new[] { "1♣", "Pass", "1♥", "Pass", "2♥", "Pass" },
                hand = "AS5S AHQHTH9H QC7C3C2C JD3D2D", // 13 HCP
                bid = "Pass", // There may be a better bid here but we used to bid 4S, which was definitely wrong
                type = "Responder Rebid"
            },

            //  Advancer Rebid

            new BasicTest
            {
                history = new[] { "1♠", "X", "Pass", "4♣", "Pass" },
                hand = "ASQS AHJH9H KDJDTD9D ACKCJC6C5C", // 19 HCP
                bid = "Pass", // There may be a better bid here but we used to bid 4S "asking for more information" which seems wrong since partner already indicated a suit (and probably shouldn't be bid this high)
                type = "Advancer Rebid"
            },
          
            new BasicTest
            {
                history = new[] { "1NT", "Pass", "2♣", "Pass", "2♦", "Pass", },
                hand = "ASKSTS2S KH3H4H5H 5D6D7D 8C9C", // 10 HCP
                bid = "3NT",
                type = "Stayman Responder rebid"
            }
            //  Blackwood

            //  Gerber
        };

        public class BasicTest
        {
            public string bid { get; set; }
            public string hand { get; set; }
            public string[] history { get; set; }
            public string type { get; set; }
        }
    }
}