using Bogus;

namespace SMACD.Scanner.Helpers
{
    internal static class JargonGenerator
    {
        private static readonly string[] VERBS =
        {
            "aggregate", "architect", "benchmark", "brand", "cultivate", "deliver", "deploy", "disintermediate",
            "drive", "e-enable", "embrace", "empower", "enable", "engage", "engineer", "enhance", "envisioneer",
            "evolve", "expedite", "exploit", "extend", "facilitate", "generate", "grow", "harness", "implement",
            "incentivize", "incubate", "innovate", "integrate", "iterate", "leverage", "matrix", "maximize", "mesh",
            "monetize", "morph", "optimize", "orchestrate", "productize", "econtextualize", "redefine",
            "reintermediate", "reinvent", "repurpose", "revolutionize", "scale", "seize", "strategize", "streamline",
            "syndicate", "synergize", "synthesize", "target", "transform", "transition", "unleash", "utilize",
            "visualize", "whiteboard"
        };

        private static readonly string[] ADJECTIVES =
        {
            "24/365", "24/7", "B2B", "B2C", "back-end", "best-of-breed", "bleeding-edge", "bricks-and-clicks",
            "clicks-and-mortar", "collaborative", "compelling", "cross-platform", "cross-media", "customized",
            "cutting-edge", "distributed", "dot-com", "dynamic", "e-business", "efficient", "end-to-end", "enterprise",
            "extensible", "frictionless", "front-end", "global", "granular", "holistic", "impactful", "innovative",
            "integrated", "interactive", "intuitive", "killer", "leading-edge", "magnetic", "mission-critical",
            "next-generation", "one-to-one", "open-source", "out-of-the-box", "plug-and-play", "proactive", "real-time",
            "revolutionary", "rich", "robust", "scalable", "seamless", "sticky", "strategic", "synergistic",
            "transparent", "turn-key", "ubiquitous", "user-centric", "value-added", "vertical", "viral", "virtual",
            "visionary", "web-enabled", "wireless", "world-class"
        };

        private static readonly string[] NOUNS =
        {
            "action-items", "applications", "architectures", "bandwidth", "channels", "communities", "content",
            "convergence", "deliverables", "e-business", "e-commerce", "e-markets", "e-services", "e-tailers",
            "experiences", "eyeballs", "functionalities", "infomediaries", "infrastructures", "initiatives",
            "interfaces", "markets", "methodologies", "metrics", "mindshare", "models", "networks", "niches",
            "paradigms", "partnerships", "platforms", "portals", "relationships", "ROI", "synergies", "web-readiness",
            "schemas", "solutions", "supply-chains", "systems", "technologies", "users", "vortals", "web services"
        };

        internal static string GenerateVerbAdjNounJargon(bool capitalizeFirstLetter = true)
        {
            var str = new Faker().PickRandom(VERBS) + " " +
                      new Faker().PickRandom(ADJECTIVES) + " " +
                      new Faker().PickRandom(NOUNS);
            return capitalizeFirstLetter ? FixCase(str) : str;
        }

        internal static string GenerateAdjNounJargon(bool capitalizeFirstLetter = true)
        {
            var str = new Faker().PickRandom(ADJECTIVES) + " " +
                      new Faker().PickRandom(NOUNS);
            return capitalizeFirstLetter ? FixCase(str) : str;
        }

        internal static string GenerateMultiPartJargon()
        {
            var partA = GenerateVerbAdjNounJargon();
            var i = new Faker().Random.Number(2);
            switch (i)
            {
                case 0: // <a> with <b>
                    var wordsLikeWith = new[] { "with", "using" };
                    return $"{partA} {new Faker().PickRandom(wordsLikeWith)} {GenerateAdjNounJargon(false)}";

                case 1: // <a> to <b>
                    var wordsLikeAnd = new[] { "and", "to" };
                    return $"{partA} {new Faker().PickRandom(wordsLikeAnd)} {GenerateVerbAdjNounJargon(false)}";

                default: // <a>, <b>, and <c>
                    return $"{partA}, {GenerateAdjNounJargon(false)}, and {GenerateAdjNounJargon(false)}";
            }
        }

        private static string FixCase(string str)
        {
            var sentences = str.Split(". ");
            for (var i = 0; i < sentences.Length; i++)
                sentences[i] = char.ToUpper(sentences[i].Trim()[0]) + sentences[i].Trim().Substring(1);
            return string.Join(". ", sentences);
        }
    }
}