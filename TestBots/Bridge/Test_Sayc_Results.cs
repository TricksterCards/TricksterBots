// last updated 1/29/2023 1:30 PM (-08:00)
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
                    new SaycResult(true, 412),
                    new SaycResult(true, 412),
                    new SaycResult(true, 412),
                    new SaycResult(true, 412),
                    new SaycResult(true, 412),
                    new SaycResult(true, 413),
                    new SaycResult(true, 412)
                }
             },
             {
                "test_open_two_nt", new[]
                {
                    new SaycResult(true, 420)
                }
             },
             {
                "test_weak_game_jump_over_one_nt", new[]
                {
                    new SaycResult(false, 431),
                    new SaycResult(false, 420)
                }
             },
             {
                "test_minimum_stayman", new[]
                {
                    new SaycResult(false, -2),
                    new SaycResult(true, 428),
                    new SaycResult(true, 428),
                    new SaycResult(true, 439),
                    new SaycResult(true, 420),
                    new SaycResult(false, 440),
                    new SaycResult(false, 439),
                    new SaycResult(true, 428),
                    new SaycResult(true, 440),
                    new SaycResult(true, 428),
                    new SaycResult(true, 422)
                }
             },
             {
                "test_invitational_stayman", new[]
                {
                    new SaycResult(true, 431),
                    new SaycResult(true, 440),
                    new SaycResult(true, 439),
                    new SaycResult(true, 424),
                    new SaycResult(true, 432)
                }
             },
             {
                "test_3c_stayman", new[]
                {
                    new SaycResult(true, 429),
                    new SaycResult(true, 432),
                    new SaycResult(false, 432),
                    new SaycResult(true, 440)
                }
             },
             {
                "test_escape_route_stayman", new[]
                {
                    new SaycResult(false, 423),
                    new SaycResult(false, 432),
                    new SaycResult(false, 431),
                    new SaycResult(false, 423),
                    new SaycResult(false, 432),
                    new SaycResult(false, 431),
                    new SaycResult(false, 423),
                    new SaycResult(false, 432),
                    new SaycResult(false, 431)
                }
             },
             {
                "test_jacoby_transfers", new[]
                {
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 420),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 428),
                    new SaycResult(false, 428),
                    new SaycResult(false, 428),
                    new SaycResult(false, 432),
                    new SaycResult(false, 432),
                    new SaycResult(false, 428),
                    new SaycResult(false, 428),
                    new SaycResult(true, 432),
                    new SaycResult(true, -2),
                    new SaycResult(true, 430),
                    new SaycResult(true, 429),
                    new SaycResult(true, 432),
                    new SaycResult(false, -2),
                    new SaycResult(true, 430),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2)
                }
             },
             {
                "test_invitational_two_nt_over_one_nt", new[]
                {
                    new SaycResult(true, 420),
                    new SaycResult(true, 420),
                    new SaycResult(false, 420),
                    new SaycResult(true, -2)
                }
             },
             {
                "test_three_level_calls_over_one_nt", new[]
                {
                    new SaycResult(false, 423),
                    new SaycResult(false, 423),
                    new SaycResult(false, 423)
                }
             },
             {
                "test_slam_invitations_over_one_nt", new[]
                {
                    new SaycResult(false, 424),
                    new SaycResult(false, 422),
                    new SaycResult(false, 428),
                    new SaycResult(false, 428),
                    new SaycResult(false, 431),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 444),
                    new SaycResult(false, 452),
                    new SaycResult(true, 452)
                }
             },
             {
                "test_interference_over_one_nt", new[]
                {
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 423),
                    new SaycResult(false, 422),
                    new SaycResult(false, 422),
                    new SaycResult(true, 423),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 423),
                    new SaycResult(true, 423),
                    new SaycResult(true, 431),
                    new SaycResult(false, 423),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 424),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2)
                }
             },
             {
                "test_rule_of_twenty_open", new[]
                {
                    new SaycResult(true, 413),
                    new SaycResult(true, 415),
                    new SaycResult(true, -2),
                    new SaycResult(true, 414),
                    new SaycResult(true, 415),
                    new SaycResult(true, 414),
                    new SaycResult(true, 414),
                    new SaycResult(true, 413),
                    new SaycResult(true, 413),
                    new SaycResult(true, 414),
                    new SaycResult(true, 414),
                    new SaycResult(true, 415),
                    new SaycResult(true, 414),
                    new SaycResult(true, 416),
                    new SaycResult(true, -2)
                }
             },
             {
                "test_third_and_fourth_seat_opens", new[]
                {
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 415),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 422)
                }
             },
             {
                "test_minimum_response_to_one_of_a_major", new[]
                {
                    new SaycResult(true, 424),
                    new SaycResult(true, 415),
                    new SaycResult(true, 412),
                    new SaycResult(true, 424),
                    new SaycResult(true, 423),
                    new SaycResult(true, 412),
                    new SaycResult(true, 440),
                    new SaycResult(true, -2)
                }
             },
             {
                "test_invitational_response_to_one_of_a_major", new[]
                {
                    new SaycResult(true, 415),
                    new SaycResult(true, 422),
                    new SaycResult(true, 421),
                    new SaycResult(true, 432),
                    new SaycResult(false, 440)
                }
             },
             {
                "test_game_forcing_resonse_to_one_of_a_major", new[]
                {
                    new SaycResult(true, 415),
                    new SaycResult(true, 440),
                    new SaycResult(true, 422),
                    new SaycResult(false, 421)
                }
             },
             {
                "test_jacoby_two_nt_response_to_one_of_a_major", new[]
                {
                    new SaycResult(true, 420),
                    new SaycResult(false, 431),
                    new SaycResult(false, 431),
                    new SaycResult(true, 440),
                    new SaycResult(true, 429),
                    new SaycResult(true, 432),
                    new SaycResult(true, 428),
                    new SaycResult(false, 429),
                    new SaycResult(false, 429),
                    new SaycResult(false, 428),
                    new SaycResult(false, 428),
                    new SaycResult(true, 421),
                    new SaycResult(true, 420),
                    new SaycResult(true, 429),
                    new SaycResult(true, 429)
                }
             },
             {
                "test_slam_zone_responses_to_one_of_a_major", new[]
                {
                    new SaycResult(true, 429),
                    new SaycResult(true, 430)
                }
             },
             {
                "test_minimum_response_to_one_of_a_minor", new[]
                {
                    new SaycResult(true, 415),
                    new SaycResult(true, 415),
                    new SaycResult(true, 412),
                    new SaycResult(true, 412),
                    new SaycResult(true, 421),
                    new SaycResult(true, 412),
                    new SaycResult(false, 415),
                    new SaycResult(false, 415),
                    new SaycResult(true, 416),
                    new SaycResult(true, 415),
                    new SaycResult(true, 416),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2)
                }
             },
             {
                "test_invitational_response_to_one_of_a_minor", new[]
                {
                    new SaycResult(true, 415),
                    new SaycResult(false, -2),
                    new SaycResult(true, 414),
                    new SaycResult(true, 422),
                    new SaycResult(true, 429),
                    new SaycResult(true, 421),
                    new SaycResult(false, 416),
                    new SaycResult(false, 445),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2)
                }
             },
             {
                "test_game_forcing_response_to_one_of_a_minor", new[]
                {
                    new SaycResult(false, 414),
                    new SaycResult(true, 420),
                    new SaycResult(true, 416),
                    new SaycResult(true, 416),
                    new SaycResult(true, 416),
                    new SaycResult(true, 416),
                    new SaycResult(true, 428),
                    new SaycResult(true, 428),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 429),
                    new SaycResult(true, 421)
                }
             },
             {
                "test_slam_zone_response_to_one_of_a_minor", new[]
                {
                    new SaycResult(true, 423),
                    new SaycResult(true, 423),
                    new SaycResult(true, 424),
                    new SaycResult(false, 421)
                }
             },
             {
                "test_minimum_rebid_by_opener", new[]
                {
                    new SaycResult(false, 415),
                    new SaycResult(true, 415),
                    new SaycResult(false, 424),
                    new SaycResult(true, 424),
                    new SaycResult(true, 421),
                    new SaycResult(false, 431),
                    new SaycResult(true, 423),
                    new SaycResult(true, -2),
                    new SaycResult(false, 416),
                    new SaycResult(false, 423),
                    new SaycResult(false, 429)
                }
             },
             {
                "test_invitational_rebid_by_opener", new[]
                {
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 420),
                    new SaycResult(true, 420),
                    new SaycResult(true, 430),
                    new SaycResult(true, 432),
                    new SaycResult(true, 424),
                    new SaycResult(false, 421),
                    new SaycResult(false, 432),
                    new SaycResult(false, 428),
                    new SaycResult(true, 429),
                    new SaycResult(false, 421),
                    new SaycResult(false, -2),
                    new SaycResult(false, 439),
                    new SaycResult(true, 432),
                    new SaycResult(false, 429)
                }
             },
             {
                "test_game_forcing_rebid_by_opener", new[]
                {
                    new SaycResult(false, 431),
                    new SaycResult(true, 423),
                    new SaycResult(false, 428),
                    new SaycResult(false, -2),
                    new SaycResult(false, 429),
                    new SaycResult(false, 424),
                    new SaycResult(false, 423),
                    new SaycResult(false, 440),
                    new SaycResult(false, -2),
                    new SaycResult(false, 420),
                    new SaycResult(false, 423),
                    new SaycResult(false, 439)
                }
             },
             {
                "test_opener_rebid_after_a_limit_raise", new[]
                {
                    new SaycResult(false, 439),
                    new SaycResult(true, 439),
                    new SaycResult(true, 439),
                    new SaycResult(true, 440),
                    new SaycResult(true, 440),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 439),
                    new SaycResult(false, 440),
                    new SaycResult(false, 439),
                    new SaycResult(true, 440)
                }
             },
             {
                "test_reverses", new[]
                {
                    new SaycResult(true, 424),
                    new SaycResult(false, 432),
                    new SaycResult(false, 424),
                    new SaycResult(true, 430),
                    new SaycResult(true, 420),
                    new SaycResult(true, 422),
                    new SaycResult(true, 422),
                    new SaycResult(false, -2),
                    new SaycResult(false, 429),
                    new SaycResult(false, 430),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 430),
                    new SaycResult(false, -2),
                    new SaycResult(false, 446)
                }
             },
             {
                "test_subsequent_bidding_by_responder", new[]
                {
                    new SaycResult(true, -2),
                    new SaycResult(true, 412),
                    new SaycResult(true, 424),
                    new SaycResult(true, 421),
                    new SaycResult(false, 429),
                    new SaycResult(false, -2),
                    new SaycResult(true, 429),
                    new SaycResult(true, 420),
                    new SaycResult(true, 432),
                    new SaycResult(true, 430),
                    new SaycResult(true, -2),
                    new SaycResult(true, 422),
                    new SaycResult(false, 432),
                    new SaycResult(false, -2),
                    new SaycResult(true, 420),
                    new SaycResult(false, -2),
                    new SaycResult(true, 423),
                    new SaycResult(false, 422),
                    new SaycResult(true, 440),
                    new SaycResult(true, 424),
                    new SaycResult(true, 432),
                    new SaycResult(true, 428),
                    new SaycResult(true, 439),
                    new SaycResult(true, 446),
                    new SaycResult(false, -2),
                    new SaycResult(true, 440),
                    new SaycResult(false, -2),
                    new SaycResult(true, 429),
                    new SaycResult(true, 430),
                    new SaycResult(false, 439),
                    new SaycResult(true, 439),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 420),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 437),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 430),
                    new SaycResult(false, 424),
                    new SaycResult(false, -2),
                    new SaycResult(false, 429),
                    new SaycResult(false, 423)
                }
             },
             {
                "test_fourth_suit_forcing", new[]
                {
                    new SaycResult(false, 429),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 439),
                    new SaycResult(true, 431),
                    new SaycResult(false, -2),
                    new SaycResult(true, 439),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 412),
                    new SaycResult(true, 424),
                    new SaycResult(false, 429),
                    new SaycResult(true, 429),
                    new SaycResult(false, 420),
                    new SaycResult(false, 420),
                    new SaycResult(true, 420),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 429),
                    new SaycResult(true, 439),
                    new SaycResult(false, 424),
                    new SaycResult(false, -2)
                }
             },
             {
                "test_preemption", new[]
                {
                    new SaycResult(true, 423),
                    new SaycResult(true, 423),
                    new SaycResult(true, 423),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 415),
                    new SaycResult(true, 415),
                    new SaycResult(true, 415),
                    new SaycResult(true, 415),
                    new SaycResult(false, 415),
                    new SaycResult(true, 422),
                    new SaycResult(true, 430),
                    new SaycResult(false, 414),
                    new SaycResult(false, 423),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 430),
                    new SaycResult(true, 431),
                    new SaycResult(false, 420),
                    new SaycResult(true, 432),
                    new SaycResult(true, 420),
                    new SaycResult(true, 428),
                    new SaycResult(true, 432),
                    new SaycResult(true, 430),
                    new SaycResult(true, 430),
                    new SaycResult(true, 432),
                    new SaycResult(false, 431),
                    new SaycResult(false, 431),
                    new SaycResult(true, 431),
                    new SaycResult(true, 415),
                    new SaycResult(true, 432),
                    new SaycResult(true, 432),
                    new SaycResult(true, 432),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 430),
                    new SaycResult(true, 430),
                    new SaycResult(true, 430),
                    new SaycResult(true, -2),
                    new SaycResult(true, 429),
                    new SaycResult(true, 429),
                    new SaycResult(true, 429),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 432),
                    new SaycResult(true, 432),
                    new SaycResult(true, 432),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 445),
                    new SaycResult(true, 431),
                    new SaycResult(false, 439),
                    new SaycResult(true, 439),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 429),
                    new SaycResult(false, 431),
                    new SaycResult(true, -2),
                    new SaycResult(false, 432),
                    new SaycResult(false, 432),
                    new SaycResult(false, -2),
                    new SaycResult(false, 429),
                    new SaycResult(true, 439),
                    new SaycResult(true, 439),
                    new SaycResult(true, 439),
                    new SaycResult(true, 415),
                    new SaycResult(true, 415),
                    new SaycResult(false, 416),
                    new SaycResult(false, 416),
                    new SaycResult(false, 432),
                    new SaycResult(false, -2),
                    new SaycResult(false, 446),
                    new SaycResult(false, -2),
                    new SaycResult(false, 415)
                }
             },
             {
                "test_preemptive_overcalls", new[]
                {
                    new SaycResult(true, 415),
                    new SaycResult(true, 416),
                    new SaycResult(true, 416),
                    new SaycResult(true, 423),
                    new SaycResult(true, 424),
                    new SaycResult(true, 424),
                    new SaycResult(true, 422),
                    new SaycResult(true, 430),
                    new SaycResult(true, 438),
                    new SaycResult(false, 415)
                }
             },
             {
                "test_strong_two_club", new[]
                {
                    new SaycResult(true, 421),
                    new SaycResult(true, 421),
                    new SaycResult(true, 421),
                    new SaycResult(false, 415),
                    new SaycResult(true, 423),
                    new SaycResult(true, 430),
                    new SaycResult(true, 420),
                    new SaycResult(true, 422),
                    new SaycResult(true, 423),
                    new SaycResult(false, 431),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 437),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 423),
                    new SaycResult(true, 428),
                    new SaycResult(true, 424),
                    new SaycResult(false, 432),
                    new SaycResult(true, -2)
                }
             },
             {
                "test_overcalls", new[]
                {
                    new SaycResult(true, 416),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 415),
                    new SaycResult(true, 415),
                    new SaycResult(true, 415),
                    new SaycResult(true, 423),
                    new SaycResult(true, 423),
                    new SaycResult(false, 402),
                    new SaycResult(true, 416),
                    new SaycResult(false, 424),
                    new SaycResult(true, 424),
                    new SaycResult(false, 424),
                    new SaycResult(false, -2),
                    new SaycResult(true, 424),
                    new SaycResult(false, 421),
                    new SaycResult(false, 402),
                    new SaycResult(true, 412),
                    new SaycResult(true, 412),
                    new SaycResult(true, 412),
                    new SaycResult(true, 424),
                    new SaycResult(true, 412),
                    new SaycResult(false, -2),
                    new SaycResult(true, 412),
                    new SaycResult(true, 412),
                    new SaycResult(true, 402),
                    new SaycResult(true, 432),
                    new SaycResult(true, 424),
                    new SaycResult(true, 421),
                    new SaycResult(true, 421),
                    new SaycResult(false, -2),
                    new SaycResult(false, 423),
                    new SaycResult(false, 432),
                    new SaycResult(true, 424),
                    new SaycResult(true, 432),
                    new SaycResult(true, 440)
                }
             },
             {
                "test_michaels_and_unusual_notrump", new[]
                {
                    new SaycResult(false, 421),
                    new SaycResult(true, 421),
                    new SaycResult(true, 422),
                    new SaycResult(false, -2),
                    new SaycResult(true, 421),
                    new SaycResult(true, 422),
                    new SaycResult(false, 424),
                    new SaycResult(false, -2),
                    new SaycResult(false, 421),
                    new SaycResult(false, 422),
                    new SaycResult(true, 421),
                    new SaycResult(true, 422),
                    new SaycResult(false, -2),
                    new SaycResult(false, 423),
                    new SaycResult(false, 423),
                    new SaycResult(false, 423),
                    new SaycResult(true, 437),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 416),
                    new SaycResult(false, 422),
                    new SaycResult(false, 422),
                    new SaycResult(false, 416),
                    new SaycResult(false, 416),
                    new SaycResult(true, -2),
                    new SaycResult(true, 423),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 431),
                    new SaycResult(true, 420),
                    new SaycResult(false, 402),
                    new SaycResult(false, 402),
                    new SaycResult(false, 420),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 402),
                    new SaycResult(true, 422),
                    new SaycResult(false, 402),
                    new SaycResult(false, 420),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2)
                }
             },
             {
                "test_responses_to_michaels", new[]
                {
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 429)
                }
             },
             {
                "test_overcalling_one_notrump", new[]
                {
                    new SaycResult(false, -2),
                    new SaycResult(false, 424),
                    new SaycResult(true, 424),
                    new SaycResult(true, 423),
                    new SaycResult(false, 421),
                    new SaycResult(false, 421),
                    new SaycResult(false, 420),
                    new SaycResult(false, -2),
                    new SaycResult(false, 429),
                    new SaycResult(false, 446),
                    new SaycResult(false, 438),
                    new SaycResult(false, 420),
                    new SaycResult(false, 446),
                    new SaycResult(true, 429),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 432),
                    new SaycResult(false, 424),
                    new SaycResult(false, 423),
                    new SaycResult(true, 424),
                    new SaycResult(false, 422),
                    new SaycResult(false, 423),
                    new SaycResult(false, 422),
                    new SaycResult(false, -2)
                }
             },
             {
                "test_doubles", new[]
                {
                    new SaycResult(false, 423),
                    new SaycResult(true, 423),
                    new SaycResult(false, 437),
                    new SaycResult(true, 424),
                    new SaycResult(true, 402),
                    new SaycResult(true, 402),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 420),
                    new SaycResult(true, 415),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 402),
                    new SaycResult(true, 424),
                    new SaycResult(true, 402),
                    new SaycResult(false, -2),
                    new SaycResult(true, 423),
                    new SaycResult(true, 423),
                    new SaycResult(true, 423),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 402),
                    new SaycResult(true, 402),
                    new SaycResult(true, 402),
                    new SaycResult(true, 402),
                    new SaycResult(false, 402),
                    new SaycResult(false, -2),
                    new SaycResult(true, 431),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 424),
                    new SaycResult(true, 416),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 424),
                    new SaycResult(false, 416),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 421),
                    new SaycResult(true, 432),
                    new SaycResult(true, 412),
                    new SaycResult(false, 415),
                    new SaycResult(false, 429),
                    new SaycResult(true, 428),
                    new SaycResult(true, 439),
                    new SaycResult(false, 415),
                    new SaycResult(false, 423),
                    new SaycResult(true, 423),
                    new SaycResult(false, 415),
                    new SaycResult(true, 424),
                    new SaycResult(true, 440),
                    new SaycResult(false, 412),
                    new SaycResult(true, 415),
                    new SaycResult(false, 432),
                    new SaycResult(false, 424),
                    new SaycResult(false, 439),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2)
                }
             },
             {
                "test_negative_double", new[]
                {
                    new SaycResult(true, 402),
                    new SaycResult(true, 402),
                    new SaycResult(true, 402),
                    new SaycResult(true, -2),
                    new SaycResult(true, 402),
                    new SaycResult(false, 424),
                    new SaycResult(false, 455),
                    new SaycResult(false, 445),
                    new SaycResult(false, 454),
                    new SaycResult(false, 430),
                    new SaycResult(true, 440),
                    new SaycResult(true, 439),
                    new SaycResult(true, 420),
                    new SaycResult(false, 440),
                    new SaycResult(false, 439),
                    new SaycResult(false, 412),
                    new SaycResult(true, 421),
                    new SaycResult(true, 412),
                    new SaycResult(false, -2),
                    new SaycResult(false, 422),
                    new SaycResult(true, 420),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 421),
                    new SaycResult(false, 424),
                    new SaycResult(false, 424),
                    new SaycResult(false, 412),
                    new SaycResult(false, 423),
                    new SaycResult(true, 424),
                    new SaycResult(true, 402),
                    new SaycResult(false, -2),
                    new SaycResult(true, 402),
                    new SaycResult(true, 415),
                    new SaycResult(true, 415),
                    new SaycResult(true, 402),
                    new SaycResult(true, 402),
                    new SaycResult(false, -2),
                    new SaycResult(true, 402),
                    new SaycResult(false, 402),
                    new SaycResult(true, 402),
                    new SaycResult(true, 422),
                    new SaycResult(false, 421),
                    new SaycResult(false, 423),
                    new SaycResult(true, 402),
                    new SaycResult(true, 423),
                    new SaycResult(false, -2),
                    new SaycResult(true, 423),
                    new SaycResult(true, 402),
                    new SaycResult(false, 431),
                    new SaycResult(false, 429),
                    new SaycResult(false, 437),
                    new SaycResult(false, -2),
                    new SaycResult(false, 415)
                }
             },
             {
                "test_reopening_double", new[]
                {
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 423),
                    new SaycResult(false, 424),
                    new SaycResult(false, 420),
                    new SaycResult(true, 420),
                    new SaycResult(false, 423),
                    new SaycResult(false, -2),
                    new SaycResult(false, 421),
                    new SaycResult(false, 424)
                }
             },
             {
                "test_balancing", new[]
                {
                    new SaycResult(false, -2),
                    new SaycResult(false, 416),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 415),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 421),
                    new SaycResult(false, 428),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 415),
                    new SaycResult(true, 415),
                    new SaycResult(false, 412),
                    new SaycResult(false, -2),
                    new SaycResult(true, 423),
                    new SaycResult(true, 422),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 421),
                    new SaycResult(true, 423),
                    new SaycResult(false, 421),
                    new SaycResult(true, 423),
                    new SaycResult(false, 423),
                    new SaycResult(false, 428),
                    new SaycResult(false, 428),
                    new SaycResult(false, 428)
                }
             },
             {
                "test_slam_biding", new[]
                {
                    new SaycResult(true, 436),
                    new SaycResult(true, 452),
                    new SaycResult(true, 436),
                    new SaycResult(false, 444),
                    new SaycResult(false, 440),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 463),
                    new SaycResult(true, 447)
                }
             },
             {
                "test_misc_hands_from_play", new[]
                {
                    new SaycResult(false, -2),
                    new SaycResult(true, 423),
                    new SaycResult(true, 415),
                    new SaycResult(true, 440),
                    new SaycResult(true, 431),
                    new SaycResult(false, 422),
                    new SaycResult(true, 440),
                    new SaycResult(true, 428),
                    new SaycResult(false, 428),
                    new SaycResult(false, -2),
                    new SaycResult(false, 422),
                    new SaycResult(false, 421),
                    new SaycResult(true, -2),
                    new SaycResult(true, 440),
                    new SaycResult(true, -2),
                    new SaycResult(true, 416),
                    new SaycResult(true, 415),
                    new SaycResult(true, -2),
                    new SaycResult(true, 440),
                    new SaycResult(true, 422),
                    new SaycResult(true, -2),
                    new SaycResult(false, 412),
                    new SaycResult(true, 428),
                    new SaycResult(true, 440),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 415),
                    new SaycResult(true, 428),
                    new SaycResult(true, 423),
                    new SaycResult(true, -2),
                    new SaycResult(true, 424),
                    new SaycResult(false, -2),
                    new SaycResult(true, 423),
                    new SaycResult(true, -2),
                    new SaycResult(true, 428),
                    new SaycResult(true, -2),
                    new SaycResult(false, 429),
                    new SaycResult(false, -2),
                    new SaycResult(false, 422),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 415),
                    new SaycResult(true, 415),
                    new SaycResult(true, -2),
                    new SaycResult(false, 432),
                    new SaycResult(false, 424),
                    new SaycResult(false, 415),
                    new SaycResult(true, 439),
                    new SaycResult(true, 424),
                    new SaycResult(false, 423),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 414),
                    new SaycResult(true, -2),
                    new SaycResult(true, 439),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 430),
                    new SaycResult(false, 424),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 412),
                    new SaycResult(false, 423),
                    new SaycResult(true, 412),
                    new SaycResult(false, -2),
                    new SaycResult(false, 421),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 452),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 421),
                    new SaycResult(true, -2),
                    new SaycResult(true, 430),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 432),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 438),
                    new SaycResult(false, -2),
                    new SaycResult(true, 432),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 430),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 412),
                    new SaycResult(true, 422),
                    new SaycResult(false, -2),
                    new SaycResult(false, 428),
                    new SaycResult(false, 428),
                    new SaycResult(false, -2),
                    new SaycResult(true, 440),
                    new SaycResult(false, -2),
                    new SaycResult(true, 424),
                    new SaycResult(true, -2),
                    new SaycResult(false, 453),
                    new SaycResult(false, 428),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 420),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, 412),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 423),
                    new SaycResult(false, -2),
                    new SaycResult(true, 431),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 440),
                    new SaycResult(false, 430),
                    new SaycResult(false, 429),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 416),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 431),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, -2),
                    new SaycResult(false, 416),
                    new SaycResult(false, -2),
                    new SaycResult(true, 428),
                    new SaycResult(false, -2),
                    new SaycResult(true, -2),
                    new SaycResult(true, 439),
                    new SaycResult(true, 415),
                    new SaycResult(true, -2),
                    new SaycResult(true, -2),
                    new SaycResult(false, 420),
                    new SaycResult(false, 422),
                    new SaycResult(true, 402),
                    new SaycResult(false, -2),
                    new SaycResult(true, 420),
                    new SaycResult(true, 428),
                    new SaycResult(false, -2),
                    new SaycResult(true, 412),
                    new SaycResult(false, 430)
                }
             },
        };
    }
}
