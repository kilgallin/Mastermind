using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mastermind
{
    // Implementation of the game "Mastermind".
    class Program
    {
        // The set of valid characters for the user to input when making a code
        // or guess.
        static char[] legalChars = new char[] { '1', '2', '3', '4', '5', '6' };
        // The length of the codes to be generated.
        const int length = 4;

        // Program entry point. Repeatedly prompts the user for options and
        // launches a game, until the user decides to quit.
        static void Main(string[] args)
        {
            // Main program loop. Exits if the user presses 'n' between rounds.
            while (true)
            {
                // Make- or break-code selection.
                string selection = getInput("Would you like to [m]ake or " +
                    "[b]reak a code?", "Please enter m or b.", 1, 
                    new char[] { 'm', 'b' });
                bool makeCode = (selection[0] == 'm');

                // Number of tries to be allowed for a game.
                int tries;
                Console.WriteLine("How many tries do you want to play with?");

                // Get the number of tries to play with this round, with 
                // error-checking.
                try
                {
                    int.TryParse(Console.ReadLine(), out tries);
                    if (tries < 1)
                    {
                        Console.WriteLine("Please enter a valid integer");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    continue;
                }

                // Play the game with the options chosen.
                if (makeCode)
                {
                    playAsCodemaker(tries);
                }
                else
                {
                    playAsCodebreaker(tries);
                }

                // Ask about playing again. Repeat loop if so and exit if not.
                string repeat = getInput("Play again? y/n", 
                    "Please enter y or n.", 1, new char[] { 'y', 'n' });
                if (repeat == "n")
                {
                    return;
                }
            }
        }

        // Prompt user for console input, read it, and validate it.
        private static string getInput(string prompt, string reprompt, 
        int expectedLength, char[] expectedChars)
        {
            Console.WriteLine(prompt);
            while (true)
            {
                string input = Console.ReadLine();
                if (input.Length != expectedLength || !input.All<char>(
                    delegate(char c) { return expectedChars.Contains(c); }))
                {
                    Console.WriteLine(reprompt);
                    continue;
                }
                return input;
            }
        }

        // Allow the user to make a code and have the AI try to break it.
        private static void playAsCodemaker(int tries)
        {
            // Get code to be cracked.
            string answer = getInput("Please enter a 4-digit code for the " +
                "computer to crack.", "Please enter 4 digits between 1 and 6.",
                4, legalChars);
            
            // Allow the AI to attempt to guess code.
            Solver solver = new Solver(tries);
            string score = "";

            // Until code is solved or tries are exhausted, get a guess, score
            // and print it, and report the score to the AI on the next round.
            for (int i = 0; i < tries; i++)
            {
                string guess = solver.getGuess(score);
                score = scoreEntry(guess, answer);
                Console.WriteLine("Guess " + (i + 1) + ": " + guess + 
                    ", score: " + score);

                if (guess == answer)
                {
                    Console.WriteLine("AI solved it!");
                    break;
                }
            }
        }

        // Main game logic. Takes the number of tries the user is to be allowed.
        private static void playAsCodebreaker(int tries)
        {
            // Used to generate a solution.
            Random r = new Random();
            string answer = "";
            // Generate four digits in the range 1-6.
            for (int i = 0; i < length; i++)
            {
                answer += r.Next(1, 7);
            }

            // Prompt the user for guesses until the attempts are exhausted.
            for (int usedTries = 0; usedTries < tries; usedTries++)
            {
                string input = getInput("Please enter your guess", 
                    "Please enter 4 digits between 1 and 6", 4, legalChars);
                
                // Check game win condition.
                if (answer == input)
                {
                    Console.WriteLine("You solved it!");
                    return;
                }

                // Get result and write + and - signs.
                Console.WriteLine(scoreEntry(input, answer));
            }
            
            // If the user exhausts their tries, they lose.
            Console.WriteLine("You lose :(");
            Console.WriteLine("The solution was: " + answer);
        }

        // Compare a guess against an answer and compute the string of '+'/'-'.
        public static string scoreEntry(string guess, string answer)
        {
            string ret = "";
            // A mutable copy of the answer string. Entries in this array are 
            // replaced with '0' when they are matched, in order to satisfy the
            // constraint that each position in the solution is only matched
            // once for '+'/'-' output on a guess. Entries in the guess are 
            // replaced with '+' when matched to avoid matching again.
            char[] tempAnswer = answer.ToCharArray();

            // Check value+position matches.
            for (int i = 0; i < length; i++)
            {
                if (guess[i] == tempAnswer[i])
                {
                    ret += '+';
                    // Mark this position as used
                    tempAnswer[i] = '0';
                    // Replace the character in the guess so that it cannot 
                    // trigger value-only matches. Arbitrarily using '+' for 
                    // this as well; the input string goes out of scope soon
                    // so the exact character is irrelevent.
                    StringBuilder sb = new StringBuilder(guess);
                    sb[i] = '+';
                    guess = sb.ToString();
                }
            }

            // Check value-only matches.
            for (int i = 0; i < length; i++)
            {
                int index = Array.IndexOf(tempAnswer, guess[i]);
                if (index >= 0)
                {
                    ret += '-';
                    // Mark this position as used.
                    tempAnswer[index] = '0';
                }
            }

            return ret;
        }
    }

    // An implementation of a basic Mastermind AI.
    class Solver
    {
        // Number of attempts already made.
        int numGuessesTried;
        // Set of guesses that are still viable.
        HashSet<string> remainingGuesses;
        // Guesses already made, indexed by the round guessed on.
        string[] guesses;
        // +/- scoring for guesses already made, also indexed by round.
        string[] scores;

        // Initialize data structures and populate remainingGuesses with the
        // 1296 length-4 guesses by building the guesses one character at a
        // time, alternating storage between remainingGuesses and tempGuessSet.
        // At the end of this process, remainingGuesses contains the 1296 
        // length-4 strings desired.
        public Solver(int tries)
        {
            scores = new string[tries];
            guesses = new string[tries];
            remainingGuesses = new HashSet<string>();
            HashSet<string> tempGuessSet = new HashSet<string>();
            // Length-1 strings.
            for (int i = 1; i <= 6; i++)
            {
                tempGuessSet.Add(i.ToString());
            }
            // Length-2 strings.
            extendStrings(tempGuessSet, remainingGuesses);
            tempGuessSet.Clear();
            // Length-3 strings.
            extendStrings(remainingGuesses, tempGuessSet);
            remainingGuesses.Clear();
            // Length-4 strings.
            extendStrings(tempGuessSet, remainingGuesses);
        }

        // Takes the strings in source, appends each possible next digit, and
        // adds the result to the target set.
        private void extendStrings(HashSet<string> source, 
        HashSet<string> target)
        {
            foreach (string str in source)
            {
                for (int i = 1; i <= 6; i++)
                {
                    target.Add(str + i.ToString());
                }
            }
        }

        // Loosely follows Knuth's 5-round algorithm for Mastermind (see 
        // wikipedia entry for Mastermind). Makes an initial guess of "1122",
        // then repeatedly tries the lexicographically first guess that conforms
        // to the results already obtained for previous guesses. For example, if
        // "1122" scores a "++--", then "6666" could not possibly be the
        // solution but "1212" and "2121" could.
        public string getGuess(string prevScore)
        {
            // Initial guess
            if (numGuessesTried == 0)
            {
                numGuessesTried++;
                guesses[0] = "1122";
                return "1122";
            }
            // Subsequent guesses.
            else
            {
                // Record previous score.
                scores[numGuessesTried - 1] = prevScore;
                // Avoid modifying the collection while iterating through it.
                HashSet<string> tempGuessSet = 
                    new HashSet<string>(remainingGuesses);
                string guess = guesses[numGuessesTried - 1];
                string score = scores[numGuessesTried - 1];

                // Check each currently-viable guess against the result from the
                // last round and remove non-matches.
                foreach (string str in tempGuessSet)
                {
                    if (Program.scoreEntry(guess, str) != score)
                    {
                        remainingGuesses.Remove(str);
                    }
                }

                // Take the first remaining guess, remove it to avoid making
                // the same guess twice, record it, and return it.
                string ret = remainingGuesses.ElementAt(0);
                remainingGuesses.Remove(ret);
                guesses[numGuessesTried] = ret;
                numGuessesTried++;
                return ret;
            }
        }
    }
}