// last updated 10/20/2025 4:48 PM (-05:00)
using System.Collections.Generic;

namespace TestBots
{
    public static class Test_Sayc_Results
    {
        public static readonly Dictionary<string, SaycResult[]> Results = new Dictionary<string, SaycResult[]>
        {
             {
                "test_open_one_nt", new[]
                {
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 413, 413),
                    new SaycResult(true, 412, 412),
                }
             },
             {
                "test_open_two_nt", new[]
                {
                    new SaycResult(false, 421, 420), // last run result: 2♣; expected: 2NT;
                }
             },
             {
                "test_weak_game_jump_over_one_nt", new[]
                {
                    new SaycResult(false, 431, 439), // last run result: 3♠; expected: 4♠;
                    new SaycResult(false, 420, -2), // last run result: 2NT; expected: Pass;
                }
             },
             {
                "test_minimum_stayman", new[]
                {
                    new SaycResult(false, 428, 402), // last run result: 3NT; expected: X;
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 431, 431),
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 440, 440),
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 422, 422),
                }
             },
             {
                "test_invitational_stayman", new[]
                {
                    new SaycResult(true, 431, 431),
                    new SaycResult(false, 431, 440), // last run result: 3♠; expected: 4♥;
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 432, 432),
                }
             },
             {
                "test_3c_stayman", new[]
                {
                    new SaycResult(true, 429, 429),
                    new SaycResult(true, 432, 432),
                    new SaycResult(false, 431, 402), // last run result: 3♠; expected: X;
                    new SaycResult(false, 431, 440), // last run result: 3♠; expected: 4♥;
                }
             },
             {
                "test_escape_route_stayman", new[]
                {
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                }
             },
             {
                "test_jacoby_transfers", new[]
                {
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 420, 420),
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(false, -2, 432), // last run result: Pass; expected: 3♥;
                    new SaycResult(true, 428, 428),
                    new SaycResult(false, 428, 440), // last run result: 3NT; expected: 4♥;
                    new SaycResult(false, 428, 432), // last run result: 3NT; expected: 3♥;
                    new SaycResult(false, 432, 440), // last run result: 3♥; expected: 4♥;
                    new SaycResult(false, 432, 440), // last run result: 3♥; expected: 4♥;
                    new SaycResult(false, 428, 430), // last run result: 3NT; expected: 3♦;
                    new SaycResult(false, 428, 429), // last run result: 3NT; expected: 3♣;
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, 429, 429),
                    new SaycResult(false, 439, 432), // last run result: 4♠; expected: 3♥;
                    new SaycResult(true, 440, 440),
                    new SaycResult(false, 440, 430), // last run result: 4♥; expected: 3♦;
                    new SaycResult(true, 438, 438),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 432), // last run result: Pass; expected: 3♥;
                }
             },
             {
                "test_invitational_two_nt_over_one_nt", new[]
                {
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 420, 420),
                    new SaycResult(false, 420, -2), // last run result: 2NT; expected: Pass;
                    new SaycResult(true, -2, -2),
                }
             },
             {
                "test_three_level_calls_over_one_nt", new[]
                {
                    new SaycResult(true, 429, 429),
                    new SaycResult(true, 430, 430),
                    new SaycResult(false, 423, 428), // last run result: 2♠; expected: 3NT;
                }
             },
             {
                "test_slam_invitations_over_one_nt", new[]
                {
                    new SaycResult(true, 431, 431),
                    new SaycResult(true, 432, 432),
                    new SaycResult(false, 445, 429), // last run result: 5♣; expected: 3♣;
                    new SaycResult(false, 446, 430), // last run result: 5♦; expected: 3♦;
                    new SaycResult(false, 455, 439), // last run result: 6♠; expected: 4♠;
                    new SaycResult(true, 446, 446),
                    new SaycResult(true, 444, 444),
                    new SaycResult(true, 447, 447),
                    new SaycResult(true, 448, 448),
                    new SaycResult(true, 446, 446),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 444, 444),
                    new SaycResult(true, 452, 452),
                }
             },
             {
                "test_interference_over_one_nt", new[]
                {
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 422), // last run result: Pass; expected: 2♦;
                    new SaycResult(false, -2, 421), // last run result: Pass; expected: 2♣;
                    new SaycResult(true, 423, 423),
                    new SaycResult(false, 422, -2), // last run result: 2♦; expected: Pass;
                    new SaycResult(false, 422, 403), // last run result: 2♦; expected: XX;
                    new SaycResult(true, 423, 423),
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, -2, 403), // last run result: Pass; expected: XX;
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 431, 431),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 432), // last run result: Pass; expected: 3♥;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 424, 424),
                    new SaycResult(false, 420, 422), // last run result: 2NT; expected: 2♦;
                    new SaycResult(false, 420, -2), // last run result: 2NT; expected: Pass;
                }
             },
             {
                "test_rule_of_twenty_open", new[]
                {
                    new SaycResult(true, 413, 413),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 414, 414),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 414, 414),
                    new SaycResult(true, 414, 414),
                    new SaycResult(true, 413, 413),
                    new SaycResult(true, 413, 413),
                    new SaycResult(true, 414, 414),
                    new SaycResult(true, 414, 414),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 414, 414),
                    new SaycResult(true, 416, 416),
                    new SaycResult(true, -2, -2),
                }
             },
             {
                "test_third_and_fourth_seat_opens", new[]
                {
                    new SaycResult(false, -2, 413), // last run result: Pass; expected: 1♣;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 422, 431), // last run result: 2♦; expected: 3♠;
                }
             },
             {
                "test_minimum_response_to_one_of_a_major", new[]
                {
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 440, 440),
                    new SaycResult(true, -2, -2),
                }
             },
             {
                "test_invitational_response_to_one_of_a_major", new[]
                {
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 422, 422),
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 432, 432),
                    new SaycResult(false, 420, 421), // last run result: 2NT; expected: 2♣;
                }
             },
             {
                "test_game_forcing_resonse_to_one_of_a_major", new[]
                {
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 440, 440),
                    new SaycResult(true, 422, 422),
                    new SaycResult(false, 421, 428), // last run result: 2♣; expected: 3NT;
                }
             },
             {
                "test_jacoby_two_nt_response_to_one_of_a_major", new[]
                {
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 438, 438),
                    new SaycResult(false, 438, 432), // last run result: 4♦; expected: 3♥;
                    new SaycResult(true, 440, 440),
                    new SaycResult(true, 429, 429),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 438, 438),
                    new SaycResult(true, 438, 438),
                    new SaycResult(false, 428, 420), // last run result: 3NT; expected: 2NT;
                    new SaycResult(false, 428, 420), // last run result: 3NT; expected: 2NT;
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 429, 429),
                    new SaycResult(true, 429, 429),
                }
             },
             {
                "test_slam_zone_responses_to_one_of_a_major", new[]
                {
                    new SaycResult(true, 429, 429),
                    new SaycResult(true, 430, 430),
                }
             },
             {
                "test_minimum_response_to_one_of_a_minor", new[]
                {
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 412, 412),
                    new SaycResult(false, 415, 414), // last run result: 1♠; expected: 1♦;
                    new SaycResult(false, 415, 414), // last run result: 1♠; expected: 1♦;
                    new SaycResult(true, 416, 416),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 416, 416),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 429, 445), // last run result: 3♣; expected: 5♣;
                }
             },
             {
                "test_invitational_response_to_one_of_a_minor", new[]
                {
                    new SaycResult(true, 415, 415),
                    new SaycResult(false, 422, 412), // last run result: 2♦; expected: 1NT;
                    new SaycResult(true, 414, 414),
                    new SaycResult(true, 422, 422),
                    new SaycResult(true, 429, 429),
                    new SaycResult(true, 421, 421),
                    new SaycResult(false, 416, 422), // last run result: 1♥; expected: 2♦;
                    new SaycResult(false, 445, 428), // last run result: 5♣; expected: 3NT;
                    new SaycResult(true, 412, 412),
                    new SaycResult(false, 422, 430), // last run result: 2♦; expected: 3♦;
                }
             },
             {
                "test_game_forcing_response_to_one_of_a_minor", new[]
                {
                    new SaycResult(false, 414, 420), // last run result: 1♦; expected: 2NT;
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 416, 416),
                    new SaycResult(true, 416, 416),
                    new SaycResult(true, 416, 416),
                    new SaycResult(true, 416, 416),
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 428, 428),
                    new SaycResult(false, 429, 437), // last run result: 3♣; expected: 4♣;
                    new SaycResult(false, 430, 438), // last run result: 3♦; expected: 4♦;
                    new SaycResult(false, 429, 437), // last run result: 3♣; expected: 4♣;
                    new SaycResult(false, 429, 421), // last run result: 3♣; expected: 2♣;
                    new SaycResult(true, 421, 421),
                }
             },
             {
                "test_slam_zone_response_to_one_of_a_minor", new[]
                {
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 424, 424),
                    new SaycResult(false, 421, 420), // last run result: 2♣; expected: 2NT;
                }
             },
             {
                "test_minimum_rebid_by_opener", new[]
                {
                    new SaycResult(false, 415, 424), // last run result: 1♠; expected: 2♥;
                    new SaycResult(true, 415, 415),
                    new SaycResult(false, 424, 412), // last run result: 2♥; expected: 1NT;
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 421, 421),
                    new SaycResult(false, 431, 423), // last run result: 3♠; expected: 2♠;
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 416, -2), // last run result: 1♥; expected: Pass;
                    new SaycResult(false, 423, 430), // last run result: 2♠; expected: 3♦;
                    new SaycResult(false, 429, 422), // last run result: 3♣; expected: 2♦;
                }
             },
             {
                "test_invitational_rebid_by_opener", new[]
                {
                    new SaycResult(false, -2, 439), // last run result: Pass; expected: 4♠;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 431, 431),
                    new SaycResult(false, 420, 423), // last run result: 2NT; expected: 2♠;
                    new SaycResult(false, 428, 420), // last run result: 3NT; expected: 2NT;
                    new SaycResult(true, 429, 429),
                    new SaycResult(false, 421, 420), // last run result: 2♣; expected: 2NT;
                    new SaycResult(false, -2, 422), // last run result: Pass; expected: 2♦;
                    new SaycResult(false, 439, 429), // last run result: 4♠; expected: 3♣;
                    new SaycResult(true, 432, 432),
                    new SaycResult(false, 429, 421), // last run result: 3♣; expected: 2♣;
                }
             },
             {
                "test_game_forcing_rebid_by_opener", new[]
                {
                    new SaycResult(false, 420, 439), // last run result: 2NT; expected: 4♠;
                    new SaycResult(true, 423, 423),
                    new SaycResult(false, 428, 440), // last run result: 3NT; expected: 4♥;
                    new SaycResult(false, -2, 440), // last run result: Pass; expected: 4♥;
                    new SaycResult(false, 429, 420), // last run result: 3♣; expected: 2NT;
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, 429, 429),
                    new SaycResult(false, 440, 438), // last run result: 4♥; expected: 4♦;
                    new SaycResult(false, -2, 432), // last run result: Pass; expected: 3♥;
                    new SaycResult(false, 420, 428), // last run result: 2NT; expected: 3NT;
                    new SaycResult(false, 422, 430), // last run result: 2♦; expected: 3♦;
                    new SaycResult(false, 439, 440), // last run result: 4♠; expected: 4♥;
                }
             },
             {
                "test_opener_rebid_after_a_limit_raise", new[]
                {
                    new SaycResult(false, 439, -2), // last run result: 4♠; expected: Pass;
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 440, 440),
                    new SaycResult(true, 440, 440),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 432), // last run result: Pass; expected: 3♥;
                    new SaycResult(true, 439, 439),
                    new SaycResult(false, 440, -2), // last run result: 4♥; expected: Pass;
                    new SaycResult(false, 439, -2), // last run result: 4♠; expected: Pass;
                    new SaycResult(true, 440, 440),
                }
             },
             {
                "test_reverses", new[]
                {
                    new SaycResult(true, 424, 424),
                    new SaycResult(false, 432, 423), // last run result: 3♥; expected: 2♠;
                    new SaycResult(false, 424, 432), // last run result: 2♥; expected: 3♥;
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 422, 422),
                    new SaycResult(true, 422, 422),
                    new SaycResult(false, -2, 431), // last run result: Pass; expected: 3♠;
                    new SaycResult(false, 429, 420), // last run result: 3♣; expected: 2NT;
                    new SaycResult(false, 420, 423), // last run result: 2NT; expected: 2♠;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(false, -2, 429), // last run result: Pass; expected: 3♣;
                    new SaycResult(false, -2, 431), // last run result: Pass; expected: 3♠;
                    new SaycResult(true, 420, 420),
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(false, 446, 429), // last run result: 5♦; expected: 3♣;
                }
             },
             {
                "test_subsequent_bidding_by_responder", new[]
                {
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 421, 421),
                    new SaycResult(false, 429, -2), // last run result: 3♣; expected: Pass;
                    new SaycResult(false, -2, 424), // last run result: Pass; expected: 2♥;
                    new SaycResult(true, 429, 429),
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 422, 422),
                    new SaycResult(false, 432, 424), // last run result: 3♥; expected: 2♥;
                    new SaycResult(false, -2, 421), // last run result: Pass; expected: 2♣;
                    new SaycResult(true, 420, 420),
                    new SaycResult(false, -2, 424), // last run result: Pass; expected: 2♥;
                    new SaycResult(true, 423, 423),
                    new SaycResult(false, 422, 430), // last run result: 2♦; expected: 3♦;
                    new SaycResult(true, 440, 440),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 446, 446),
                    new SaycResult(false, -2, 440), // last run result: Pass; expected: 4♥;
                    new SaycResult(true, 440, 440),
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(true, 429, 429),
                    new SaycResult(true, 430, 430),
                    new SaycResult(false, 439, 431), // last run result: 4♠; expected: 3♠;
                    new SaycResult(true, 439, 439),
                    new SaycResult(false, -2, 430), // last run result: Pass; expected: 3♦;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(false, 420, -2), // last run result: 2NT; expected: Pass;
                    new SaycResult(false, -2, 440), // last run result: Pass; expected: 4♥;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 437, 428), // last run result: 4♣; expected: 3NT;
                    new SaycResult(false, -2, 440), // last run result: Pass; expected: 4♥;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 430, 415), // last run result: 3♦; expected: 1♠;
                    new SaycResult(false, 424, 428), // last run result: 2♥; expected: 3NT;
                    new SaycResult(false, -2, 439), // last run result: Pass; expected: 4♠;
                    new SaycResult(false, 432, 424), // last run result: 3♥; expected: 2♥;
                    new SaycResult(false, 423, 430), // last run result: 2♠; expected: 3♦;
                }
             },
             {
                "test_fourth_suit_forcing", new[]
                {
                    new SaycResult(false, 429, 422), // last run result: 3♣; expected: 2♦;
                    new SaycResult(false, -2, 431), // last run result: Pass; expected: 3♠;
                    new SaycResult(false, -2, 431), // last run result: Pass; expected: 3♠;
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 431, 431),
                    new SaycResult(false, -2, 437), // last run result: Pass; expected: 4♣;
                    new SaycResult(true, 439, 439),
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(false, -2, 437), // last run result: Pass; expected: 4♣;
                    new SaycResult(false, -2, 438), // last run result: Pass; expected: 4♦;
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(false, 412, 415), // last run result: 1NT; expected: 1♠;
                    new SaycResult(true, 424, 424),
                    new SaycResult(false, 429, 440), // last run result: 3♣; expected: 4♥;
                    new SaycResult(true, 429, 429),
                    new SaycResult(false, 420, 428), // last run result: 2NT; expected: 3NT;
                    new SaycResult(false, 420, 428), // last run result: 2NT; expected: 3NT;
                    new SaycResult(true, 420, 420),
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(false, -2, 422), // last run result: Pass; expected: 2♦;
                    new SaycResult(false, 429, 430), // last run result: 3♣; expected: 3♦;
                    new SaycResult(true, 439, 439),
                    new SaycResult(false, 424, 423), // last run result: 2♥; expected: 2♠;
                    new SaycResult(false, -2, 430), // last run result: Pass; expected: 3♦;
                }
             },
             {
                "test_preemption", new[]
                {
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 415, 415),
                    new SaycResult(false, 415, 423), // last run result: 1♠; expected: 2♠;
                    new SaycResult(true, 422, 422),
                    new SaycResult(true, 430, 430),
                    new SaycResult(false, 414, 438), // last run result: 1♦; expected: 4♦;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 430, -2), // last run result: 3♦; expected: Pass;
                    new SaycResult(true, 431, 431),
                    new SaycResult(false, 420, 430), // last run result: 2NT; expected: 3♦;
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, 432, 432),
                    new SaycResult(false, 431, -2), // last run result: 3♠; expected: Pass;
                    new SaycResult(false, 431, -2), // last run result: 3♠; expected: Pass;
                    new SaycResult(true, 431, 431),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 429), // last run result: Pass; expected: 3♣;
                    new SaycResult(false, -2, 429), // last run result: Pass; expected: 3♣;
                    new SaycResult(false, -2, 429), // last run result: Pass; expected: 3♣;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 445, 445),
                    new SaycResult(true, 431, 431),
                    new SaycResult(false, 439, 428), // last run result: 4♠; expected: 3NT;
                    new SaycResult(true, 439, 439),
                    new SaycResult(false, -2, 439), // last run result: Pass; expected: 4♠;
                    new SaycResult(false, -2, 447), // last run result: Pass; expected: 5♠;
                    new SaycResult(false, -2, 455), // last run result: Pass; expected: 6♠;
                    new SaycResult(false, 429, 420), // last run result: 3♣; expected: 2NT;
                    new SaycResult(false, 431, 428), // last run result: 3♠; expected: 3NT;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 432, 428), // last run result: 3♥; expected: 3NT;
                    new SaycResult(false, 432, 428), // last run result: 3♥; expected: 3NT;
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(false, 432, 420), // last run result: 3♥; expected: 2NT;
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 415, 415),
                    new SaycResult(false, 416, 440), // last run result: 1♥; expected: 4♥;
                    new SaycResult(false, 416, 440), // last run result: 1♥; expected: 4♥;
                    new SaycResult(false, 432, 440), // last run result: 3♥; expected: 4♥;
                    new SaycResult(false, -2, 440), // last run result: Pass; expected: 4♥;
                    new SaycResult(false, -2, 445), // last run result: Pass; expected: 5♣;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, 415, 423), // last run result: 1♠; expected: 2♠;
                }
             },
             {
                "test_preemptive_overcalls", new[]
                {
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 416, 416),
                    new SaycResult(true, 416, 416),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 422, 422),
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, 438, 438),
                    new SaycResult(false, 415, 423), // last run result: 1♠; expected: 2♠;
                }
             },
             {
                "test_strong_two_club", new[]
                {
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 421, 421),
                    new SaycResult(false, 415, 421), // last run result: 1♠; expected: 2♣;
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 422, 422),
                    new SaycResult(true, 423, 423),
                    new SaycResult(false, 431, 439), // last run result: 3♠; expected: 4♠;
                    new SaycResult(false, -2, 455), // last run result: Pass; expected: 6♠;
                    new SaycResult(false, -2, 445), // last run result: Pass; expected: 5♣;
                    new SaycResult(true, 431, 431),
                    new SaycResult(false, -2, 439), // last run result: Pass; expected: 4♠;
                    new SaycResult(true, 431, 431),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 423, 431), // last run result: 2♠; expected: 3♠;
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 424, 424),
                    new SaycResult(false, 432, -2), // last run result: 3♥; expected: Pass;
                    new SaycResult(true, -2, -2),
                }
             },
             {
                "test_overcalls", new[]
                {
                    new SaycResult(true, 416, 416),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 423, 423),
                    new SaycResult(false, 402, 416), // last run result: X; expected: 1♥;
                    new SaycResult(true, 416, 416),
                    new SaycResult(false, 424, -2), // last run result: 2♥; expected: Pass;
                    new SaycResult(true, 424, 424),
                    new SaycResult(false, 424, -2), // last run result: 2♥; expected: Pass;
                    new SaycResult(false, -2, 424), // last run result: Pass; expected: 2♥;
                    new SaycResult(true, 424, 424),
                    new SaycResult(false, 421, 415), // last run result: 2♣; expected: 1♠;
                    new SaycResult(false, 402, 424), // last run result: X; expected: 2♥;
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 412, 412),
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 421, 421),
                    new SaycResult(false, -2, 432), // last run result: Pass; expected: 3♥;
                    new SaycResult(false, 423, 415), // last run result: 2♠; expected: 1♠;
                    new SaycResult(false, 432, 422), // last run result: 3♥; expected: 2♦;
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 440, 440),
                }
             },
             {
                "test_michaels_and_unusual_notrump", new[]
                {
                    new SaycResult(false, 421, -2), // last run result: 2♣; expected: Pass;
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 422, 422),
                    new SaycResult(false, -2, 439), // last run result: Pass; expected: 4♠;
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 422, 422),
                    new SaycResult(false, 424, 415), // last run result: 2♥; expected: 1♠;
                    new SaycResult(false, -2, 430), // last run result: Pass; expected: 3♦;
                    new SaycResult(false, 421, 415), // last run result: 2♣; expected: 1♠;
                    new SaycResult(false, 422, 415), // last run result: 2♦; expected: 1♠;
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 422, 422),
                    new SaycResult(false, -2, 422), // last run result: Pass; expected: 2♦;
                    new SaycResult(false, 423, 421), // last run result: 2♠; expected: 2♣;
                    new SaycResult(false, 423, 422), // last run result: 2♠; expected: 2♦;
                    new SaycResult(false, 423, 432), // last run result: 2♠; expected: 3♥;
                    new SaycResult(true, 437, 437),
                    new SaycResult(false, -2, 437), // last run result: Pass; expected: 4♣;
                    new SaycResult(false, -2, 438), // last run result: Pass; expected: 4♦;
                    new SaycResult(false, -2, 437), // last run result: Pass; expected: 4♣;
                    new SaycResult(false, 416, 420), // last run result: 1♥; expected: 2NT;
                    new SaycResult(false, 422, 420), // last run result: 2♦; expected: 2NT;
                    new SaycResult(false, 422, 420), // last run result: 2♦; expected: 2NT;
                    new SaycResult(false, 416, 420), // last run result: 1♥; expected: 2NT;
                    new SaycResult(false, 416, -2), // last run result: 1♥; expected: Pass;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 431, 431),
                    new SaycResult(true, 420, 420),
                    new SaycResult(false, 402, 436), // last run result: X; expected: 4NT;
                    new SaycResult(false, 402, 436), // last run result: X; expected: 4NT;
                    new SaycResult(false, 420, -2), // last run result: 2NT; expected: Pass;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 402, 420), // last run result: X; expected: 2NT;
                    new SaycResult(true, 422, 422),
                    new SaycResult(false, 402, 420), // last run result: X; expected: 2NT;
                    new SaycResult(false, 420, 416), // last run result: 2NT; expected: 1♥;
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, 432, 432),
                }
             },
             {
                "test_responses_to_michaels", new[]
                {
                    new SaycResult(false, -2, 429), // last run result: Pass; expected: 3♣;
                    new SaycResult(false, -2, 430), // last run result: Pass; expected: 3♦;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 438), // last run result: Pass; expected: 4♦;
                    new SaycResult(true, 445, 445),
                    new SaycResult(false, 445, 446), // last run result: 5♣; expected: 5♦;
                    new SaycResult(true, 432, 432),
                    new SaycResult(false, 429, 420), // last run result: 3♣; expected: 2NT;
                }
             },
             {
                "test_overcalling_one_notrump", new[]
                {
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 422, 422),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 420, 420),
                    new SaycResult(false, 421, 429), // last run result: 2♣; expected: 3♣;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 422, 422),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 432, 432),
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(true, 429, 429),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 429, 429),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 424, 424),
                    new SaycResult(false, -2, 430), // last run result: Pass; expected: 3♦;
                    new SaycResult(false, -2, 431), // last run result: Pass; expected: 3♠;
                    new SaycResult(false, -2, 430), // last run result: Pass; expected: 3♦;
                    new SaycResult(false, -2, 424), // last run result: Pass; expected: 2♥;
                }
             },
             {
                "test_doubles", new[]
                {
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 420, 402), // last run result: 2NT; expected: X;
                    new SaycResult(true, 415, 415),
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 402, 402),
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 423, 423),
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 402, 402),
                    new SaycResult(false, 402, 415), // last run result: X; expected: 1♠;
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(true, 431, 431),
                    new SaycResult(false, -2, 412), // last run result: Pass; expected: 1NT;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(false, -2, 421), // last run result: Pass; expected: 2♣;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(false, -2, 412), // last run result: Pass; expected: 1NT;
                    new SaycResult(false, -2, 422), // last run result: Pass; expected: 2♦;
                    new SaycResult(false, 424, 423), // last run result: 2♥; expected: 2♠;
                    new SaycResult(true, 416, 416),
                    new SaycResult(false, -2, 412), // last run result: Pass; expected: 1NT;
                    new SaycResult(false, -2, 431), // last run result: Pass; expected: 3♠;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(false, -2, 430), // last run result: Pass; expected: 3♦;
                    new SaycResult(false, 424, 431), // last run result: 2♥; expected: 3♠;
                    new SaycResult(false, 416, 424), // last run result: 1♥; expected: 2♥;
                    new SaycResult(false, -2, 439), // last run result: Pass; expected: 4♠;
                    new SaycResult(false, -2, 446), // last run result: Pass; expected: 5♦;
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 412, 412),
                    new SaycResult(false, 440, 422), // last run result: 4♥; expected: 2♦;
                    new SaycResult(false, 429, 420), // last run result: 3♣; expected: 2NT;
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 439, 439),
                    new SaycResult(false, 415, 431), // last run result: 1♠; expected: 3♠;
                    new SaycResult(false, 423, 415), // last run result: 2♠; expected: 1♠;
                    new SaycResult(true, 423, 423),
                    new SaycResult(false, 415, 403), // last run result: 1♠; expected: XX;
                    new SaycResult(true, 424, 424),
                    new SaycResult(false, 432, 440), // last run result: 3♥; expected: 4♥;
                    new SaycResult(false, 412, 430), // last run result: 1NT; expected: 3♦;
                    new SaycResult(true, 415, 415),
                    new SaycResult(false, 432, 420), // last run result: 3♥; expected: 2NT;
                    new SaycResult(false, 424, 432), // last run result: 2♥; expected: 3♥;
                    new SaycResult(false, 439, -2), // last run result: 4♠; expected: Pass;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                }
             },
             {
                "test_negative_double", new[]
                {
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 402, 402),
                    new SaycResult(false, 424, -2), // last run result: 2♥; expected: Pass;
                    new SaycResult(false, 455, 432), // last run result: 6♠; expected: 3♥;
                    new SaycResult(false, 445, 432), // last run result: 5♣; expected: 3♥;
                    new SaycResult(false, 454, 432), // last run result: 6♦; expected: 3♥;
                    new SaycResult(false, 430, 428), // last run result: 3♦; expected: 3NT;
                    new SaycResult(true, 440, 440),
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 420, 420),
                    new SaycResult(false, 440, 432), // last run result: 4♥; expected: 3♥;
                    new SaycResult(false, 439, 431), // last run result: 4♠; expected: 3♠;
                    new SaycResult(false, 412, 422), // last run result: 1NT; expected: 2♦;
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, 412, 412),
                    new SaycResult(false, -2, 429), // last run result: Pass; expected: 3♣;
                    new SaycResult(false, 422, 429), // last run result: 2♦; expected: 3♣;
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 428, 428),
                    new SaycResult(false, 421, 420), // last run result: 2♣; expected: 2NT;
                    new SaycResult(false, 420, 428), // last run result: 2NT; expected: 3NT;
                    new SaycResult(false, 424, 423), // last run result: 2♥; expected: 2♠;
                    new SaycResult(false, 412, 424), // last run result: 1NT; expected: 2♥;
                    new SaycResult(false, 423, 424), // last run result: 2♠; expected: 2♥;
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 402, 402),
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 402, 402),
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(true, 402, 402),
                    new SaycResult(false, 402, 424), // last run result: X; expected: 2♥;
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 422, 422),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, 402, 402),
                    new SaycResult(true, 423, 423),
                    new SaycResult(false, -2, 424), // last run result: Pass; expected: 2♥;
                    new SaycResult(false, 429, 423), // last run result: 3♣; expected: 2♠;
                    new SaycResult(true, 402, 402),
                    new SaycResult(false, 431, 402), // last run result: 3♠; expected: X;
                    new SaycResult(false, 429, 430), // last run result: 3♣; expected: 3♦;
                    new SaycResult(false, 437, 430), // last run result: 4♣; expected: 3♦;
                    new SaycResult(false, -2, 431), // last run result: Pass; expected: 3♠;
                    new SaycResult(false, 415, 423), // last run result: 1♠; expected: 2♠;
                }
             },
             {
                "test_reopening_double", new[]
                {
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(false, -2, 422), // last run result: Pass; expected: 2♦;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 423, 402), // last run result: 2♠; expected: X;
                    new SaycResult(false, 424, 402), // last run result: 2♥; expected: X;
                    new SaycResult(false, 420, -2), // last run result: 2NT; expected: Pass;
                    new SaycResult(true, 420, 420),
                    new SaycResult(false, 412, -2), // last run result: 1NT; expected: Pass;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, 421, 402), // last run result: 2♣; expected: X;
                    new SaycResult(false, 424, 432), // last run result: 2♥; expected: 3♥;
                }
             },
             {
                "test_balancing", new[]
                {
                    new SaycResult(false, -2, 424), // last run result: Pass; expected: 2♥;
                    new SaycResult(false, 416, 424), // last run result: 1♥; expected: 2♥;
                    new SaycResult(false, -2, 424), // last run result: Pass; expected: 2♥;
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 412), // last run result: Pass; expected: 1NT;
                    new SaycResult(false, -2, 421), // last run result: Pass; expected: 2♣;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, 415, 402), // last run result: 1♠; expected: X;
                    new SaycResult(true, 415, 415),
                    new SaycResult(false, 412, 402), // last run result: 1NT; expected: X;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 422, 422),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 421, 423), // last run result: 2♣; expected: 2♠;
                    new SaycResult(true, 423, 423),
                    new SaycResult(false, 421, 431), // last run result: 2♣; expected: 3♠;
                    new SaycResult(true, 423, 423),
                    new SaycResult(false, 423, 415), // last run result: 2♠; expected: 1♠;
                    new SaycResult(false, 428, -2), // last run result: 3NT; expected: Pass;
                    new SaycResult(false, -2, 421), // last run result: Pass; expected: 2♣;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                }
             },
             {
                "test_slam_biding", new[]
                {
                    new SaycResult(true, 436, 436),
                    new SaycResult(false, 444, 452), // last run result: 5NT; expected: 6NT;
                    new SaycResult(true, 436, 436),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 440, 448), // last run result: 4♥; expected: 5♥;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 448), // last run result: Pass; expected: 5♥;
                    new SaycResult(false, -2, 456), // last run result: Pass; expected: 6♥;
                    new SaycResult(false, -2, 455), // last run result: Pass; expected: 6♠;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 463, 463),
                    new SaycResult(true, 447, 447),
                }
             },
             {
                "test_misc_hands_from_play", new[]
                {
                    new SaycResult(false, -2, 421), // last run result: Pass; expected: 2♣;
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 440, 440),
                    new SaycResult(true, 431, 431),
                    new SaycResult(false, 422, 415), // last run result: 2♦; expected: 1♠;
                    new SaycResult(true, 440, 440),
                    new SaycResult(true, 428, 428),
                    new SaycResult(false, 428, 421), // last run result: 3NT; expected: 2♣;
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(false, 429, 428), // last run result: 3♣; expected: 3NT;
                    new SaycResult(false, 421, -2), // last run result: 2♣; expected: Pass;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 440, 440),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 416, 416),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 440, 440),
                    new SaycResult(false, 424, 422), // last run result: 2♥; expected: 2♦;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 412, 421), // last run result: 1NT; expected: 2♣;
                    new SaycResult(true, 428, 428),
                    new SaycResult(false, -2, 440), // last run result: Pass; expected: 4♥;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 432, 424), // last run result: 3♥; expected: 2♥;
                    new SaycResult(false, -2, 431), // last run result: Pass; expected: 3♠;
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 429, -2), // last run result: 3♣; expected: Pass;
                    new SaycResult(false, -2, 422), // last run result: Pass; expected: 2♦;
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 438, 452), // last run result: 4♦; expected: 6NT;
                    new SaycResult(false, 424, 412), // last run result: 2♥; expected: 1NT;
                    new SaycResult(false, 415, 402), // last run result: 1♠; expected: X;
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 424, 424),
                    new SaycResult(false, 423, 431), // last run result: 2♠; expected: 3♠;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(false, 414, -2), // last run result: 1♦; expected: Pass;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 439, 439),
                    new SaycResult(false, -2, 432), // last run result: Pass; expected: 3♥;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 437), // last run result: Pass; expected: 4♣;
                    new SaycResult(false, 430, 422), // last run result: 3♦; expected: 2♦;
                    new SaycResult(true, 412, 412),
                    new SaycResult(false, -2, 422), // last run result: Pass; expected: 2♦;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 440), // last run result: Pass; expected: 4♥;
                    new SaycResult(false, -2, 432), // last run result: Pass; expected: 3♥;
                    new SaycResult(false, -2, 439), // last run result: Pass; expected: 4♠;
                    new SaycResult(false, -2, 439), // last run result: Pass; expected: 4♠;
                    new SaycResult(true, 412, 412),
                    new SaycResult(false, 423, 431), // last run result: 2♠; expected: 3♠;
                    new SaycResult(true, 412, 412),
                    new SaycResult(false, -2, 444), // last run result: Pass; expected: 5NT;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 452), // last run result: Pass; expected: 6NT;
                    new SaycResult(true, 452, 452),
                    new SaycResult(false, -2, 439), // last run result: Pass; expected: 4♠;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 421, 421),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 438, 430), // last run result: 4♦; expected: 3♦;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 421), // last run result: Pass; expected: 2♣;
                    new SaycResult(false, -2, 414), // last run result: Pass; expected: 1♦;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 432, 438), // last run result: 3♥; expected: 4♦;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 438, 430), // last run result: 4♦; expected: 3♦;
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(true, 432, 432),
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 429), // last run result: Pass; expected: 3♣;
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 445), // last run result: Pass; expected: 5♣;
                    new SaycResult(true, 430, 430),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(true, 412, 412),
                    new SaycResult(true, 422, 422),
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, 428, 420), // last run result: 3NT; expected: 2NT;
                    new SaycResult(false, 428, -2), // last run result: 3NT; expected: Pass;
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, 440, 440),
                    new SaycResult(false, -2, 439), // last run result: Pass; expected: 4♠;
                    new SaycResult(true, 424, 424),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 455, 439), // last run result: 6♠; expected: 4♠;
                    new SaycResult(false, 428, 440), // last run result: 3NT; expected: 4♥;
                    new SaycResult(false, 429, 432), // last run result: 3♣; expected: 3♥;
                    new SaycResult(false, -2, 421), // last run result: Pass; expected: 2♣;
                    new SaycResult(true, 420, 420),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 422, 402), // last run result: 2♦; expected: X;
                    new SaycResult(true, 412, 412),
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(true, 428, 428),
                    new SaycResult(false, -2, 446), // last run result: Pass; expected: 5♦;
                    new SaycResult(false, 423, 420), // last run result: 2♠; expected: 2NT;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, -2, 431), // last run result: Pass; expected: 3♠;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 440, 432), // last run result: 4♥; expected: 3♥;
                    new SaycResult(false, 430, 422), // last run result: 3♦; expected: 2♦;
                    new SaycResult(true, 432, 432),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 440), // last run result: Pass; expected: 4♥;
                    new SaycResult(false, -2, 430), // last run result: Pass; expected: 3♦;
                    new SaycResult(false, -2, 423), // last run result: Pass; expected: 2♠;
                    new SaycResult(false, 416, 415), // last run result: 1♥; expected: 1♠;
                    new SaycResult(false, -2, 431), // last run result: Pass; expected: 3♠;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, -2, 422), // last run result: Pass; expected: 2♦;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 431, 431),
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 430, 430),
                    new SaycResult(false, 416, 415), // last run result: 1♥; expected: 1♠;
                    new SaycResult(false, -2, 420), // last run result: Pass; expected: 2NT;
                    new SaycResult(true, 428, 428),
                    new SaycResult(true, 423, 423),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, 439, 439),
                    new SaycResult(true, 415, 415),
                    new SaycResult(true, -2, -2),
                    new SaycResult(true, -2, -2),
                    new SaycResult(false, 420, 429), // last run result: 2NT; expected: 3♣;
                    new SaycResult(false, 423, -2), // last run result: 2♠; expected: Pass;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(false, -2, 402), // last run result: Pass; expected: X;
                    new SaycResult(true, 420, 420),
                    new SaycResult(false, -2, 428), // last run result: Pass; expected: 3NT;
                    new SaycResult(false, -2, 416), // last run result: Pass; expected: 1♥;
                    new SaycResult(true, 412, 412),
                    new SaycResult(false, 420, 424), // last run result: 2NT; expected: 2♥;
                }
             },
        };
    }
}
