using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;

namespace SMACD.Shared
{
    public static class Extensions
    {
        private const int HASH_LENGTH = 8;
        private static string[] ADJECTIVES = new[] { "round", "obeisant", "quirky", "doubtful", "extra-large", "rural", "roomy", "synonymous", "ajar", "freezing", "utter", "reflective", "full", "swift", "dizzy", "eatable", "gaping", "pricey", "sore", "chilly", "elegant", "boring", "absorbed", "stormy", "four", "null", "tame", "efficient", "hard-to-find", "ruddy", "pointless", "callous", "well-groomed", "nimble", "teeny-tiny", "puny", "exclusive", "sudden", "ready", "worthless", "black", "skinny", "decorous", "absent", "heavenly", "little", "historical", "premium", "assorted", "futuristic", "substantial", "hard-to-find", "fumbling", "incandescent", "true", "hallowed", "tense", "knotty", "furry", "tart", "fumbling", "closed", "little", "tacit", "keen", "lying", "flat", "alluring", "toothsome", "outgoing", "enchanting", "valuable", "aromatic", "cautious", "telling", "rural", "historical", "strange", "gentle", "long-term", "ubiquitous", "special", "irate", "nosy", "uppity", "mammoth", "fast", "diligent", "scientific", "petite", "judicious", "dirty", "hissing", "momentous", "petite", "super", "alive", "dear", "yummy", "unkempt", "massive", "concerned", "foolish", "shrill", "piquant", "humdrum", "possible", "sassy", "jealous", "unbiased", "luxuriant", "hollow", "somber", "invincible", "valuable", "cute", "rude", "dusty", "abaft", "slimy", "kindhearted", "joyous", "purple", "dashing", "legal", "fumbling", "guiltless", "ill", "alluring", "satisfying", "organic", "capricious", "spooky", "upbeat", "psychedelic", "fair", "uneven", "vulgar", "measly", "sleepy", "limping", "long", "lewd", "troubled", "efficacious", "angry", "exciting", "adaptable", "grotesque", "solid", "voracious", "fearful", "jolly", "spotted", "finicky", "fascinated", "marvelous", "clear", "ragged", "husky", "real", "dramatic", "skillful", "yellow", "merciful", "highfalutin", "flowery", "scared", "fine", "unnatural", "malicious", "warm", "substantial", "green", "squalid", "numberless", "romantic", "madly", "pale", "prickly", "understood", "jumbled", "spectacular", "black-and-white", "gusty", "determined", "wandering", "curly", "general", "bright", "first", "tense", "striped", "black", "purring", "nippy", "faint", "well-made", "unknown", "hollow", "helpful", "mindless", "bright", "tidy", "successful", "flowery", "unbiased", "concerned", "noisy", "opposite", "terrific", "greedy", "tenuous", "tawdry", "earsplitting", "godly", "proud", "elated", "extra-small", "petite", "godly", "able", "wiggly", "hard-to-find", "wooden", "stimulating", "laughable", "fragile", "brawny", "utter", "painstaking", "broad", "enthusiastic", "obsequious", "arrogant", "responsible", "befitting", "remarkable", "chilly", "ambitious", "purple", "tangible", "fresh", "scrawny", "open", "brown", "nimble", "outstanding", "jaded", "handsomely", "cynical", "teeny", "public", "smiling", "ludicrous", "first", "pathetic", "nutty", "sweet", "chubby", "right", "lamentable", "nauseating", "different", "motionless", "prickly", "same", "dusty", "null", "amused", "tired", "robust", "glamorous", "able", "alert", "healthy", "rural", "rampant", "loose", "filthy", "thinkable", "placid", "valuable", "pointless", "annoyed", "delicate", "black", "jagged", "cagey", "tangy", "torpid", "enormous", "tasteful", "habitual", "next", "worried", "resonant", "alive", "aspiring", "secret", "finicky", "living", "crooked", "pretty", "curious", "deep", "low", "rustic", "dysfunctional", "fertile", "ethereal", "shocking", "chilly", "youthful", "rabid", "sulky", "violent", "succinct", "fragile", "humdrum", "mammoth", "possible", "cluttered", "fine", "previous", "psychedelic", "elegant", "helpless", "fragile", "aloof", "efficient", "cautious", "complete", "eminent", "tight", "gruesome", "charming", "snobbish", "oceanic", "hallowed", "gabby", "purring", "superficial", "unusual", "wandering", "well-to-do", "square", "outrageous", "scattered", "alluring", "innocent", "caring", "low", "straight", "tightfisted", "wanting", "intelligent", "wholesale", "grandiose", "capable", "hesitant", "grouchy", "scintillating", "dusty", "inconclusive", "friendly", "silent", "abhorrent", "second-hand", "malicious", "tidy", "illegal", "certain", "superficial", "available", "erect", "overconfident", "brawny", "white", "capable", "high-pitched", "weak", "exciting", "high-pitched", "disillusioned", "obscene", "separate", "spicy", "adorable", "flimsy", "lethal", "known", "screeching", "quixotic", "oafish", "graceful", "coordinated", "hushed", "beneficial", "absorbing", "adhesive", "aromatic", "irate", "thoughtless", "daffy", "deep", "mute", "chemical", "rustic", "scandalous", "wacky", "bizarre", "itchy", "foregoing", "quickest", "low", "internal", "skillful", "tearful", "sturdy", "understood", "present", "drab", "rotten", "teeny", "forgetful", "ruthless", "odd", "wakeful", "therapeutic", "gusty", "assorted", "absurd", "full", "marked", "doubtful", "physical", "responsible", "soft", "annoying", "sassy", "ruddy", "public", "kaput", "stupendous", "tiny", "chubby", "guarded", "clear", "third", "demonic", "moaning", "befitting", "melodic", "clumsy", "unique", "ajar", "dizzy", "huge", "sticky", "axiomatic", "lonely", "earthy", "chunky", "intelligent", "holistic", "young", "faded", "tremendous", "dusty", "absent", "evanescent", "flippant", "different", "wrong", "miscreant", "brave", "unequal", "successful", "cuddly", "healthy", "milky", "obese", "uttermost", "mushy", "anxious", "cold", "humorous", "magenta", "well-groomed", "racial", "cloistered", "redundant", "elite", "unused", "afraid", "sweet", "tiresome", "worried", "round", "lamentable", "amused", "quaint", "spotless", "lackadaisical", "clumsy", "terrible", "ultra", "unruly", "solid", "auspicious", "trite", "versed", "nine", "rebel", "condemned", "wise", "tense", "nice", "small", "fast", "fallacious", "acidic", "giddy", "ruddy", "mute", "four", "outrageous", "panicky", "successful", "sulky", "threatening", "jobless", "angry", "guttural", "flippant", "needless", "lavish", "painstaking", "magical", "demonic", "phobic", "grandiose", "plastic", "concerned", "magnificent", "pleasant", "brainy", "tenuous", "uptight", "delightful", "strange", "rich", "left", "exclusive", "unbecoming", "humdrum", "even", "huge", "icy", "fantastic", "hospitable", "hanging", "clean", "glossy", "rich", "terrific", "rude", "dark", "versed", "second-hand", "sincere", "faulty", "gaudy", "ignorant", "secret", "evanescent", "breezy", "tasty", "offbeat", "adventurous", "mute", "periodic", "lumpy", "careless", "odd", "kindly", "symptomatic", "ad hoc", "naive", "sophisticated", "massive", "aboard", "godly", "subsequent", "impossible", "slimy", "towering", "aboard", "ratty", "aggressive", "hollow", "aberrant", "immense", "clear", "cooing", "cagey", "helpless", "solid", "juvenile", "nutty", "public", "poised", "tasteful", "impolite", "rotten", "unaccountable", "aback", "magnificent", "illustrious", "alluring", "half", "imaginary", "brainy", "crazy", "befitting", "nice", "undesirable", "paltry", "stingy", "private", "adventurous", "guttural", "absent", "elastic", "glamorous", "spicy", "heavenly", "weary", "ordinary", "various", "upset", "uncovered", "narrow", "innate", "feeble", "defective", "angry", "grateful", "sulky", "lamentable", "stiff", "fascinated", "fat", "glossy", "fearful", "premium", "square", "highfalutin", "jumbled", "numerous", "unsightly", "magical", "feeble", "divergent", "amused", "zonked", "ambitious", "imminent", "salty", "festive", "righteous", "wild", "long", "needless", "general", "materialistic", "elegant", "versed", "black", "alert", "humdrum", "defiant", "wiggly", "parallel", "handy", "sincere", "feeble", "vagabond", "fancy", "raspy", "spiritual", "worthless", "adventurous", "obeisant", "angry", "lackadaisical", "glossy", "ceaseless", "calm", "violet", "futuristic", "actually", "slimy", "lacking", "tacky", "successful", "profuse", "delightful", "attractive", "unkempt", "tangy", "festive", "bad", "gorgeous", "breezy", "amuck", "calm", "pretty", "tender", "axiomatic", "miscreant", "obese", "knowledgeable", "aloof", "good", "coherent", "cruel", "melted", "depressed", "animated", "alert", "old", "scrawny", "bright", "homeless", "short", "capricious", "dusty", "sour", "accessible", "languid", "honorable", "grubby", "therapeutic", "guiltless", "thankful", "two", "sincere", "handsomely", "complex", "brawny", "pretty", "gabby", "caring", "ajar", "exciting", "private", "abundant", "ill-informed", "fabulous", "thirsty", "panicky", "married", "general", "living", "homely", "warm", "paltry", "sleepy", "aromatic", "harsh", "long-term", "stupendous", "acoustic", "savory", "white", "bashful", "ill-informed", "damaged", "sore", "old-fashioned", "sedate", "greasy", "beneficial", "cold", "paltry", "tacky", "narrow", "near", "hollow", "redundant", "beneficial", "absent", "quiet", "nasty", "bored", "wise", "smelly", "spiritual", "ugliest", "cold", "curvy", "disastrous", "flawless", "crabby", "solid", "nasty", "absent", "troubled", "changeable", "magical", "gifted", "tremendous", "plausible", "taboo", "accurate", "successful", "dark", "loose", "adorable", "hollow", "stingy", "yielding", "keen", "overrated", "mammoth", "unbecoming", "harsh", "knowing", "merciful", "proud", "languid", "acrid", "whispering", "pathetic", "homeless", "hollow", "ordinary", "hysterical", "foamy", "wretched", "interesting", "arrogant", "evanescent", "wholesale", "debonair", "friendly", "psychedelic", "spectacular", "pale", "periodic", "beneficial", "shocking", "silky", "famous", "burly", "unknown", "extra-small", "mighty", "aromatic", "slow", "squealing", "medical", "neighborly", "colossal", "zany", "bawdy", "conscious", "ablaze", "cute", "icy", "didactic", "careful", "private", "chivalrous", "ajar", "callous", "thick", "lovely", "plausible", "tricky", "callous", "enthusiastic", "knowledgeable", "fair", "tacit", "lovely", "good", "flat", "sad", "mushy", "ill-fated", "selective", "striped", "garrulous", "fallacious", "clever", "industrious", "gaping", "cluttered", "domineering", "melodic", "learned", "deadpan", "vivacious", "perpetual", "jumbled", "enchanting", "wistful", "slimy", "petite", "brief", "clear", "thundering", "foolish", "exuberant", "minor", "volatile", "synonymous", "valuable", "past", "rapid", "chilly", "obsequious", "boiling", "selfish", "tan", "better", "hapless", "loose", "whimsical", "standing", "attractive", "delightful", "mixed", "skinny", "highfalutin", "romantic", "watery", "silly", "petite", "billowy", "elegant", "obscene", "miniature", "overwrought", "discreet", "polite", "jazzy", "direful", "judicious", "crazy", "reminiscent", "blue", "concerned", "simple", "descriptive", "spurious", "acceptable", "tart", "roasted", "flat", "absurd", "homeless", "changeable", "tangy", "hissing", "squealing", "friendly", "clammy", "shocking", "obese", "foamy", "stupid", "willing", "unique", "scrawny", "comfortable", "elite", "certain", "common", "long", "handsomely", "sable", "beneficial", "grouchy", "adjoining", "tidy", "alluring", "absorbed", "extra-large", "pumped", "loving", "fretful", "uppity", "sparkling", "thirsty", "giant", "high" };
        private static string[] VERBS = new[] { "business", "stream", "experience", "guide", "crowd", "flower", "arm", "bag", "move", "crime", "coil", "rainstorm", "sense", "limit", "milk", "observation", "society", "porter", "digestion", "argument", "chain", "agreement", "elbow", "structure", "copy", "bushes", "drawer", "sky", "fly", "trail", "stamp", "bed", "haircut", "K", "humor", "weight", "farm", "sponge", "insect", "nose", "teeth", "office", "icicle", "force", "fan", "drum", "meat", "bears", "ladybug", "agreement", "question", "cherry", "tendency", "industry", "adjustment", "bee", "army", "zinc", "harbor", "ring", "hook", "map", "pear", "sugar", "dog", "country", "flight", "desire", "silver", "school", "number", "business", "income", "light", "bone", "holiday", "shame", "dime", "donkey", "friend", "expert", "room", "burst", "town", "swim", "magic", "bird", "seashore", "society", "beam", "rail", "tent", "screw", "hen", "gun", "reward", "key", "smell", "sack", "shock", "jail", "knee", "power", "sack", "legs", "chickens", "ring", "beetle", "learning", "bit", "friction", "earthquake", "sweater", "tent", "kitty", "pets", "vegetable", "parcel", "friend", "brother", "prose", "rings", "rice", "key", "soap", "help", "quiver", "offer", "class", "can", "existence", "library", "cave", "cake", "week", "music", "company", "border", "food", "field", "name", "crate", "scale", "flag", "battle", "jellyfish", "powder", "dog", "seashore", "invention", "food", "net", "earth", "letter", "soap", "flavor", "wound", "shock", "cellar", "morning", "marble", "rifle", "run", "cushion", "whip", "prison", "trip", "acoustics", "friend", "thread", "night", "badge", "police", "trousers", "smile", "level", "soup", "jail", "foot", "record", "father", "adjustment", "company", "meal", "shoe", "pull", "bed", "man", "trick", "territory", "guitar", "quicksand", "wheel", "shelf", "stamp", "flock", "fact", "sweater", "crime", "ball", "toothpaste", "hat", "bed", "meeting", "letter", "gold", "neck", "comb", "size", "fight", "thought", "shoes", "bird", "minister", "baseball", "rat", "swim", "spy", "beef", "attention", "channel", "neck", "pot", "basin", "mice", "blood", "person", "vase", "chicken", "wave", "dog", "death", "surprise", "structure", "queen", "finger", "pot", "toad", "balance", "move", "wing", "yard", "theory", "jar", "kittens", "quill", "attack", "grape", "trade", "part", "son", "angle", "tree", "sail", "cobweb", "flag", "rock", "crow", "coat", "distance", "zebra", "tomatoes", "haircut", "tub", "knife", "step", "memory", "dolls", "substance", "spark", "alley", "meat", "cheese", "smash", "increase", "pull", "key", "cream", "ants", "fog", "ship", "salt", "root", "coat", "digestion", "grain", "advertisement", "skate", "space", "waves", "middle", "kitty", "aftermath", "salt", "spoon", "car", "bottle", "feather", "apparatus", "neck", "quarter", "meat", "bite", "attack", "pet", "amount", "humor", "hill", "chin", "men", "watch", "fire", "waves", "debt", "window", "act", "fifth", "jar", "fruit", "joke", "babies", "dust", "shock", "experience", "pancake", "nation", "pan", "noise", "geese", "parcel", "trade", "cook", "calculator", "committee", "mark", "growth", "relation", "song", "toe", "field", "net", "lake", "temper", "fish", "stem", "news", "cactus", "house", "shape", "road", "watch", "quicksand", "aftermath", "juice", "burst", "canvas", "slave", "shock", "song", "trade", "cushion", "balls", "parent", "board", "frog", "lip", "war", "dog", "whistle", "beef", "pain", "sofa", "slip", "activity", "music", "badge", "power", "addition", "structure", "acoustics", "grain", "thunder", "lace", "muscle", "curtain", "grandmother", "lamp", "mailbox", "committee", "mind", "lettuce", "year", "advice", "stretch", "history", "ball", "bed", "ink", "pets", "good-bye", "baseball", "silver", "cobweb", "wealth", "substance", "rate", "industry", "aunt", "mom", "plot", "hot", "furniture", "base", "throat", "minister", "notebook", "achiever", "flight", "snails", "low", "push", "band", "turkey", "toys", "profit", "partner", "afternoon", "bushes", "animal", "throne", "yarn", "weight", "bucket", "thunder", "motion", "expert", "blade", "corn", "cushion", "tendency", "women", "clocks", "dust", "jump", "verse", "boundary", "cars", "frame", "change", "toothbrush", "need", "range", "woman", "father", "account", "curve", "quarter", "jeans", "bikes", "low", "meeting", "cars", "brick", "shirt", "shelf", "nation", "drawer", "achiever", "tiger", "ornament", "hate", "range", "hole", "liquid", "tub", "boat", "market", "month", "popcorn", "giants", "page", "slave", "front", "thunder", "alarm", "goose", "team", "power", "treatment", "fifth", "title", "plot", "love", "alley", "statement", "music", "spring", "back", "flesh", "sail", "trains", "nail", "punishment", "flight", "men", "feet", "science", "cars", "wave", "heat", "wheel", "kitty", "health", "deer", "fireman", "rifle", "wine", "cart", "brothers", "glove", "can", "needle", "property", "scent", "bite", "cemetery", "sweater", "friend", "cave", "patch", "grandmother", "flag", "power", "trees", "voyage", "head", "straw", "zebra", "dock", "toy", "kitten", "feast", "animal", "chess", "bulb", "patch", "debt", "route", "surprise", "hall", "agreement", "soup", "offer", "polish", "donkey", "road", "attraction", "dogs", "visitor", "arm", "bear", "month", "thrill", "bottle", "toothbrush", "legs", "music", "tomatoes", "price", "week", "pet", "shape", "finger", "soup", "bells", "dock", "park", "magic", "game", "treatment", "sign", "throne", "lunch", "answer", "flower", "ear", "cream", "hearing", "eggs", "harmony", "scale", "cactus", "quill", "comfort", "shade", "rhythm", "dinosaurs", "slope", "arithmetic", "beggar", "spiders", "horses", "fan", "grandmother", "flight", "chance", "company", "suit", "tongue", "string", "birth", "cobweb", "club", "arithmetic", "wrench", "punishment", "spy", "gold", "mountain", "beggar", "crow", "fan", "tin", "stamp", "form", "table", "fold", "company", "sea", "chess", "flower", "industry", "society", "stretch", "cattle", "week", "advertisement", "street", "silver", "moon", "beetle", "screw", "zoo", "time", "temper", "owl", "town", "beds", "bed", "good-bye", "request", "organization", "swim", "wax", "north", "balloon", "bomb", "teeth", "son", "teeth", "story", "smell", "quiet", "root", "low", "collar", "suggestion", "cast", "talk", "market", "crush", "hall", "organization", "cast", "street", "wealth", "mark", "income", "toad", "hot", "key", "train", "bells", "bit", "night", "trees", "music", "shelf", "chair", "heart", "oranges", "afterthought", "society", "exchange", "expert", "alarm", "fight", "baseball", "color", "substance", "office", "education", "breath", "pocket", "finger", "offer", "blow", "voyage", "smoke", "edge", "unit", "bit", "stocking", "tongue", "vein", "spot", "grandmother", "stomach", "industry", "distance", "person", "ladybug", "linen", "truck", "society", "kitty", "cord", "bait", "view", "father", "judge", "faucet", "snakes", "basketball", "business", "bone", "action", "servant", "grip", "engine", "oatmeal", "game", "touch", "office", "hose", "run", "mother", "bread", "arch", "birth", "argument", "queen", "crib", "liquid", "linen", "can", "building", "clouds", "knot", "ants", "shoes", "friends", "business", "hope", "coast", "geese", "grass", "morning", "smile", "boot", "passenger", "move", "mark", "winter", "salt", "paint", "unit", "hot", "pollution", "deer", "hall", "loss", "oranges", "meal", "voice", "flock", "curtain", "lake", "dress", "toy", "leg", "bird", "time", "unit", "kettle", "island", "lake", "doctor", "activity", "clocks", "water", "hen", "office", "verse", "temper", "mountain", "park", "butter", "ice", "wealth", "screw", "cast", "mitten", "card", "hill", "arithmetic", "paint", "grip", "zephyr", "minute", "zinc", "friction", "tub", "bushes", "flight", "string", "sort", "sail", "string", "summer", "downtown", "copy", "word", "beetle", "expert", "cable", "order", "balloon", "mine", "bike", "frog", "fiction", "loaf", "aftermath", "meal", "point", "tub", "comfort", "paint", "respect", "guitar", "curve", "sack", "morning", "territory", "mask", "actor", "apple", "cast", "name", "rings", "straw", "approval", "wish", "trains", "park", "feather", "eggnog", "K", "offer", "use", "pot", "clover", "downtown", "zephyr", "houses", "stitch", "canvas", "laugh", "guitar", "joke", "work", "animal", "poison", "wealth", "cry", "chairs", "driving", "believe", "society", "week", "lawyer", "push", "tin", "heat", "day", "gold", "bag", "cemetery", "thumb", "account", "babies", "watch", "cast", "celery", "wing", "burst", "wire", "shirt", "ring", "", "parent", "lunch", "back", "light", "increase", "hose", "record", "existence", "part", "authority", "trains", "operation", "sign", "bottle", "pest", "sack", "sponge", "birthday", "desk", "van", "street", "rifle", "curve", "sack", "drawer", "activity", "twist", "watch", "touch", "toothpaste", "sponge", "cloth", "road", "detail", "game", "ground", "disgust", "flowers", "basket", "mother", "drug", "bait", "scene", "health", "temper", "crush", "leather", "zoo", "paper", "news", "lettuce", "event", "pan", "thumb", "punishment", "square", "flock", "war", "sleet", "theory", "power", "donkey", "tank", "view", "believe", "hammer", "science", "scene", "taste", "legs", "crayon", "maid", "competition", "comfort", "clover", "news", "verse", "girl", "tub", "scent", "elbow", "swim", "plot", "trade", "record", "gold", "stitch", "collar", "bit" };

        [ThreadStatic]
        public static Task CurrentTask;

        /// <summary>
        /// System-wide ILoggerFactory
        /// </summary>
        public static ILoggerFactory LogFactory { get; set; } = new LoggerFactory();

        private static Random Random { get; } = new Random((int)DateTime.Now.Ticks);

        /// <summary>
        /// Generate a random string
        /// </summary>
        /// <param name="length">Length of string</param>
        /// <returns></returns>
        public static string RandomString(int length) =>
            new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());

        /// <summary>
        /// Generate a random two-word name
        /// </summary>
        /// <returns></returns>
        public static string RandomName() => $"{ADJECTIVES[Random.Next(ADJECTIVES.Length)]}.{VERBS[Random.Next(VERBS.Length)]}";

        #region Config Attribute Helpers

        public static TProperty GetConfigAttribute<TAttribute, TProperty>(this object obj, Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute =>
            GetConfigAttributes<TAttribute, TProperty>(obj.GetType(), propertySelectionAction).FirstOrDefault();

        public static TProperty GetConfigAttribute<TParent, TAttribute, TProperty>(Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute =>
            GetConfigAttributes<TAttribute, TProperty>(typeof(TParent), propertySelectionAction).FirstOrDefault();

        public static IEnumerable<TProperty> GetConfigAttributes<TAttribute, TProperty>(this object obj, Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute =>
            GetConfigAttributes<TAttribute, TProperty>(obj.GetType(), propertySelectionAction);

        public static IEnumerable<TProperty> GetConfigAttributes<TParent, TAttribute, TProperty>(Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute =>
            GetConfigAttributes<TAttribute, TProperty>(typeof(TParent), propertySelectionAction);

        private static IEnumerable<TProperty> GetConfigAttributes<TAttribute, TProperty>(Type type, Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute
        {
            if (!type.CustomAttributes.Any(a => typeof(TAttribute).IsAssignableFrom(a.AttributeType)))
                return new List<TProperty>();
            return type.GetCustomAttributes(typeof(TAttribute), true).Cast<TAttribute>()
                .Select(a => propertySelectionAction(a));
        }

        public static TProperty GetConfigAttribute<TAttribute, TProperty>(this Type type, Func<TAttribute, TProperty> propertySelectionAction) where TAttribute : Attribute
        {
            if (!type.CustomAttributes.Any(a => typeof(TAttribute).IsAssignableFrom(a.AttributeType)))
                return default(TProperty);
            return type.GetCustomAttributes(typeof(TAttribute), true).Cast<TAttribute>().Select(a => propertySelectionAction(a)).FirstOrDefault();
        }
        #endregion

        private static readonly Dictionary<WeakReference<Task>, Tuple<bool, string>> _taskNames = new Dictionary<WeakReference<Task>, Tuple<bool, string>>();

        /// <summary>
        /// Tag a Task with some additional information
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="tagData">Tag information</param>
        /// <param name="isSystem">If the source is a system-level source</param>
        public static void Tag(this Task task, string tagData, bool isSystem = false)
        {
            if (task == null) return;
            var weakReference = ContainsTask(task);
            if (weakReference == null)
                weakReference = new WeakReference<Task>(task);
            _taskNames[weakReference] = Tuple.Create(isSystem, tagData);
        }

        /// <summary>
        /// Retrieve the data associated with a Task
        /// </summary>
        /// <param name="task">Task</param>
        /// <returns></returns>
        public static Tuple<bool, string> Tag(this Task task)
        {
            var weakReference = ContainsTask(task);
            if (weakReference == null) return null;
            return _taskNames[weakReference];
        }

        private static WeakReference<Task> ContainsTask(Task task)
        {
            foreach (var kvp in _taskNames.ToList())
            {
                if (!kvp.Key.TryGetTarget(out Task taskFromReference)) _taskNames.Remove(kvp.Key);
                else if (task == taskFromReference) return kvp.Key;
            }
            return null;
        }

        /// <summary>
        /// Gets a fingerprint hash of a given object
        /// </summary>
        /// <param name="obj">Object to hash</param>
        /// <param name="hashLength">Length of string to emit (from beginning)</param>
        /// <param name="skippedFields">Fields to skip in fingerprinting</param>
        /// <param name="serializeEphemeralData">If <c>TRUE</c> the fingerprinting process will include the ResourceId and SystemGenerated properties (not recommended)</param>
        /// <returns></returns>
        public static string Fingerprint<TObject>(this TObject obj, int hashLength = HASH_LENGTH, bool serializeEphemeralData = false, params string[] skippedFields)
        {
            var allSkippedFields = new List<string>();
            if (!serializeEphemeralData)
                allSkippedFields.AddRange(new[] { "resourceId", "systemCreated" });

            allSkippedFields.AddRange(skippedFields);
            allSkippedFields = allSkippedFields.Distinct().ToList();

            using (var sha1 = new SHA1Managed())
            {
                var str = new SerializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .WithTypeInspector(i => new SkipFieldsInspector(i, allSkippedFields.ToArray()))
                    .Build()
                    .Serialize(obj).Replace(Environment.NewLine, "\n");

                return sha1.ComputeHash(Encoding.ASCII.GetBytes(str))
                    .Aggregate(new StringBuilder(), (current, next) => current.Append(next.ToString("X2")))
                    .ToString()
                    .Substring(0, hashLength);
            }
        }
    }

    public class SkipFieldsInspector : TypeInspectorSkeleton
    {
        // Skip Resource system fields that would break a Fingerprint because they include ephemeral data
        private string[] _skippedFields;

        private readonly ITypeInspector _innerTypeDescriptor;

        public SkipFieldsInspector(ITypeInspector innerTypeDescriptor, params string[] skippedFields)
        {
            _innerTypeDescriptor = innerTypeDescriptor;
            _skippedFields = skippedFields;
        }

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container) =>
            _innerTypeDescriptor.GetProperties(type, container)
                .Where(p => !_skippedFields.Contains(p.Name));
    }
}