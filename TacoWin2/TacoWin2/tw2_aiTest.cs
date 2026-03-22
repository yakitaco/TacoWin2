using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using TacoWin2_BanInfo;
using TacoWin2_sfenIO;

namespace TacoWin2
{
    internal class AITestRunner
    {
        /// <summary>
        /// CSVファイルを読み込み、同期的に1行ずつAIの思考テストを実行・検証します。
        /// iterations: テストケースごとの実行回数（平均時間算出用）
        /// </summary>
        internal static void RunTest(string csvFilePath, tw2ai ai, string filterType = null, string filterValue = null, int iterations = 1)
        {
            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine($"[TEST ERROR] File not found: {csvFilePath}");
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(csvFilePath);
                Console.WriteLine($"[TEST START] Loaded {lines.Length} lines from {csvFilePath}");

                if (!string.IsNullOrEmpty(filterType) && !string.IsNullOrEmpty(filterValue))
                {
                    Console.WriteLine($"[TEST FILTER] Filtering by {filterType}: '{filterValue}'");
                }
                if (iterations > 1)
                {
                    Console.WriteLine($"[TEST MODE] Averaging over {iterations} iterations per test case.");
                }

                int lineNum = 0;
                int passCount = 0;
                int checkCount = 0;
                int runCount = 0;

                foreach (string line in lines)
                {
                    lineNum++;

                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    {
                        continue;
                    }

                    string[] parts = line.Split(',');
                    if (parts.Length < 9)
                    {
                        Console.WriteLine($"[TEST ERROR] Line {lineNum}: Invalid format. Expected 9 columns.");
                        continue;
                    }

                    string testName = parts[0].Trim();
                    string testGroup = parts[1].Trim();

                    if (filterType == "group" && testGroup != filterValue) continue;
                    if (filterType == "name" && testName != filterValue) continue;

                    runCount++;

                    string sfenStr = parts[2].Trim();
                    string expectedMovesStr = parts[3].Trim();
                    string[] sfenTokens = sfenStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (sfenTokens.Length < 3)
                    {
                        Console.WriteLine($"[TEST ERROR] Line {lineNum}: Invalid SFEN format.");
                        continue;
                    }

                    int depth = int.Parse(parts[4].Trim());
                    int deepMax = int.Parse(parts[5].Trim());
                    int deepsWidth = int.Parse(parts[6].Trim());
                    int mateDepth = int.Parse(parts[7].Trim());
                    int retMax = int.Parse(parts[8].Trim());

                    Console.WriteLine("----------------------------------------");
                    Console.WriteLine($"[TEST RUN] Group: [{testGroup}] | Name: {testName}");
                    Console.WriteLine($"  -> SFEN: {sfenStr}");
                    Console.WriteLine($"  -> Expected: {expectedMovesStr} | params: depth:{depth}, deepMax:{deepMax}, deepWidth:{deepsWidth}, mate:{mateDepth}, retMax:{retMax}");

                    long totalTimeMs = 0;
                    string bestMoveStr = "resign";
                    int lastBestScore = 0;

                    // AI探索タイムアウト・中断用の非同期処理キャンセル管理クラス
                    CancellationTokenSource searchCts = new CancellationTokenSource();
                    // AI詰み探索タイムアウト・中断用の非同期処理キャンセル管理クラス
                    CancellationTokenSource mateCts = new CancellationTokenSource();

                    // 指定回数ループして計測
                    for (int i = 0; i < iterations; i++)
                    {
                        // 盤面状態を毎回完全に初期化する
                        ban testBan = new ban();
                        List<kmove> kifu = new List<kmove>();
                        sfenIO.sfen2ban(ref testBan, sfenTokens[0], sfenTokens[2]);
                        testBan.renewMoveable();
                        Pturn testTurn = (sfenTokens[1] == "b") ? Pturn.Sente : Pturn.Gote;

                        kifu = new List<kmove>();
                        ai.resetHash();

                        // タイムアウトチェック時には、下記を有効にする
                        // 通常探索用Ctsをリセット
                        //searchCts?.Dispose();
                        //searchCts = new CancellationTokenSource();
                        //
                        //// 詰み探索用Ctsをリセット
                        //mateCts?.Dispose();
                        //mateCts = new CancellationTokenSource();
                        //
                        //searchCts.CancelAfter(15000);
                        //mateCts.CancelAfter(10000);

                        // デバッグ情報の出力
                        if (i == 0) // 最初のループのみ表示
                        {
                            DebugForm.instance.addMsg(testBan.debugShow());
                            DebugForm.instance.addMsg(testBan.banShow());
                        }

                        GC.Collect();
                        GC.WaitForPendingFinalizers();

                        Stopwatch sw = Stopwatch.StartNew();

                        (List<diagTbl> retMove, int bestScore) = ai.thinkMove(testTurn, testBan, kifu, depth, deepMax, deepsWidth, mateDepth, retMax, searchCts.Token, mateCts.Token);

                        sw.Stop();
                        totalTimeMs += sw.ElapsedMilliseconds;
                        lastBestScore = bestScore;

                        // 最終ループの結果を答え合わせに使用
                        if (i == iterations - 1)
                        {
                            if (retMove != null && retMove.Count > 0 && retMove[0].kmv != null && retMove[0].kmv[0].op > 0)
                            {
                                var km = retMove[0].kmv[0];
                                bestMoveStr = tw2usiIO.pos2usi(km.op, km.np, km.nari);
                            }
                        }
                    }

                    double avgTimeMs = (double)totalTimeMs / iterations;

                    string judgeResult;
                    if (expectedMovesStr == "-")
                    {
                        judgeResult = "SKIPPED";
                    } else
                    {
                        checkCount++;
                        string[] acceptableMoves = expectedMovesStr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (acceptableMoves.Contains(bestMoveStr))
                        {
                            judgeResult = "PASS";
                            passCount++;
                        } else
                        {
                            judgeResult = "FAIL";
                        }
                    }

                    string resultMsg = $"[TEST RESULT] [{judgeResult}] BestMove: {bestMoveStr}, Score: {lastBestScore}, Time: {avgTimeMs:F1} ms";
                    if (iterations > 1)
                    {
                        resultMsg += $" (Total: {totalTimeMs} ms / {iterations} runs)";
                    }

                    Console.WriteLine(resultMsg);
                }

                Console.WriteLine("========================================");
                Console.WriteLine($"[TEST END] Executed {runCount} tests. Pass Rate: {passCount} / {checkCount} (Checked)");
            } catch (Exception ex)
            {
                Console.WriteLine($"[TEST EXCEPTION] {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}