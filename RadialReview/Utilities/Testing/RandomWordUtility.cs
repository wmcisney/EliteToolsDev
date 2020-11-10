using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.Testing {
	public class RandomWordUtility {

		public static Random rnd = new Random();

		//300 random words
		public static List<string> WordList = new List<string>() {
			"Smooth","Beef","Acidic","Card","Aback","Suit","Meek","Panoramic","Gusty","Wrap","Slope","Number","Obtainable","Zippy","Shy","Winter","Pencil","Swanky","Sad","Name","Organic","Mellow","Flight","Lamentable","Influence","Possessive","Lively","Giants","Penitent","Tomatoes","Irate","Encouraging","Serious","Iron","Profit","Land","Panicky","Smash","Pickle","Functional","Worried","Overrated","Glorious","Awesome","Keen","System","Delay","Territory","Care","Nippy","Class","Woebegone","Ubiquitous","File","Friendly","Event","Notebook","Tempt","Motion","Statement","Protect","Immense","Tremendous","Religion","X-ray","Reading","Help","Rescue","Determined","Brown","Partner","Rock","Pat","Squealing","Electric","Bikes","Wealthy","Rejoice","Steadfast","Secretive","Education","Mean","Sound","Bright","Laugh","Numberless","Bruise","Business","Support","Puncture","End","Thought","Ink","Nasty","Spoil","Entertain","Like","Baby","Silent","Telephone","Art","Hydrant","Brash","Blot","Belong","Push","Enjoy","Walk","Vest","Weak","Anger","Square","Skin","Snow","Imagine","Employ","Stretch","Savory","Fire","Trouble","Prepare","Stranger","Remain","Food","Warm","Son","Cherry","Coat","Back","Quick","Dolls","Faded","Follow","Shock","Breezy","Wacky","Mine","Vein","Regular","Rhythm","Flap","Lock","Handsome","Grouchy","Luxuriant","Equal","Lively","Box","Rapid","Rinse","Wholesale","Resolute","Hushed","Question","Glamorous","Snails","Scary","Roasted","Wide-eyed","Rebel","Royal","Hobbies","Alive","Tent","Deadpan","Victorious","Plan","Uptight","Happy","Leg","Late","Bashful","Unkempt","Useful","Close","Parsimonious","Tie","Shivering","Wind","Example","Table","Sheep","Zinc","Way","Animated","Wrestle","Plant","Fold","Whip","Group","Peace","Scientific","Force","Automatic","Adjoining","Lettuce","Punish","Warm","Welcome","Whisper","Boil","Van","Plot","Point","Twig","Frighten","Second","Encourage","Dust","Calculator","Treat","Yielding","Inquisitive","Chew","Protective","Oatmeal","Bless","Recondite","Program","Motionless","Board","Excellent","Hope","Drain","Coach","Quack","Colour","Relieved","Try","Calculate","Growth","Ruin","Picayune","Farm","Threatening","Wonderful","Change","Nation","Miss","Elfin","Acoustic","Unequaled","Permissible","Noisy","Dazzling","Nappy","Rule","Press","Ship","Afraid","Stay","Gaudy","Unsuitable","Jellyfish","Coil","Party","Material","Depend","Person","Mountain","Noise","Grip","Ordinary","Windy","Tire","Force","Excite","Shrug","Cap","Flat","Chief","Paltry","Copper","Tawdry","Memory","Curl","Quickest","Last","Attraction","Harmonious","Downtown","Whip","Calendar"
		};


		public static List<string> CompanySuffix = new List<string>() {
			"Inc.","Corp.","Corporation","Incorporated","LLC", "Services","Enterprise","Holdings","Properties","Company"
		};

		public static List<string> FirstNames = new List<string>() {
			"Santina","Tessa","Wanda","Katharina","Harley","Digna","Jennefer","Gloria","Janita","Lenny","Kristel","Kimberly","Adena","Bradford","Jannette","Fabiola","Kathrin","Trang","Jonell","Milagros","Charla","Rufus","Deandre","Juan","Eliza","Vera","Annetta","Bianca","Illa","Collene","Gabriel","Ellis","Doris","Iris","Norberto","Chanda","Joya","Jesica","Carl","Wava","Reginia","Leandro","Shawnee","Renna","Pasty","Santos","Franklyn","Suellen","Gordon","Jerome","Sylvia","Johnna","Lakendra","Phillis","Evalyn","Maribeth","Lue","Tuan","Chang","Antonette","Zulema","Wynell","Lahoma","Kent","Concha","Berta","Tanna","Ruby","Debrah","Garland","Tom","Ashley","Fatima","Thomasena","Manda","Antoinette","Juan","Ellena","Dee","Jo","Allene","Joellen","Latanya","Mara","Jayne","Bunny","Enda","Dalia","Joi","Veronique","Gregoria","Crissy","Dagny","Lisbeth","Stephan","Antoine","Asa","Juliana","Anibal","Al","Loyce","Neely","Maira","Teisha","Tenesha","Thora","Norbert","Willodean","Ramonita","Keely","Annmarie","Mari","Maybelle","Benjamin","Andra","Takisha","Nicole","Renae","Vannessa","Otha","Nathanial","Cruz","Pearly","Yolando","Malorie","Jospeh","Oneida","Charlene","Jovita","Ammie","Jama","Yuonne","Debbie","Theron","Tangela","Lavone","Shirlene","Ilene","Pam","Sueann","Mira","Glenn","Ester","Luvenia","Ellyn","Ladonna","Denny","Domenica","Renea","Marquis","Shela","Myron","Jerilyn","Cary","Willow","Lamar","Chandra","Earle","Josephina","Janella","Erwin","Hermila","Brynn","Eileen","Margurite","Shena","Johnathon","Starla","Kiley","Jerica","Blair","Janeth","Ava","Ellyn","Edwina","Claretha","Candace","Chia","Torie","Valene","Shaina","Leandra","Valorie","Thuy","Melaine","Darcey","Addie","Jennell","Ethel","Sylvia","Grisel","Melania","Simon","Lakendra","Derrick","Armanda","Walter","Nida","Virgen","Emmy",
		};
		public static List<string> LastNames = new List<string>() {
			"Campbell","Traverso","Boerger","Chamlee","Sasso","Dowdell","Ovalle","Humfeld","Mifflin","Bradwell","Brodnax","Soza","Stavros","Portillo","Minnis","Concha","Riggan","Dixson","Chester","Manjarrez","Harcrow","Chatterton","Wease","Iadarola","Livengood","Mallari","Rizer","Marie","Sackett","Weibel","Varden","Elton","Bednarczyk","Hudec","Rosebrock","Pasco","Costilla","Bosch","Neaves","Fagen","Zaccaria","Wren","Lintner","Heeter","Somers","Slater","Capel","Kelch","Sydnor","Kiernan","Cooney","Constantino","Lerner","Hohl","Joslin","Olea","Dickert","Curington","Beals","Roseman","Miele","Morquecho","Domenick","Diniz","Dubuc","Stedman","Fleishman","Crittenden","Beacham","Mcmillen","Mackowiak","Kensey","Alvino","Rooney","Fenwick","Melnick","Schumacher","Linz","Zehr","Hogans","Delker","Rawson","Woodbury","Rowen","Pylant","Rusnak","Shoults","Grube","Mcclard","Lawrence","Upham","Deak","Millsaps","Cranfield","Fabry","Greenland","Duca","Reuther","Breshears","Casa","Fajardo","Schmeling","Ellen","Eisner","Peterson","Mcginnis","Hintzen","Huisman","Radtke","Shackelford","Enos","Chasteen","Thao","Mayes","Sarris","Marceau","Sthilaire","Bollig","Luoma","Shears","Galasso","Plewa","Laskowski","Schmitz","Knopp","Henriksen","Fontanilla","Shugart","Welcher","Mathis","Vantrease","Mumaw","Gott","Stocker","Carlow","Sobel","Beran","Campbell","Jorden","Morissette","Bloodworth","Stankiewicz","Castrejon","Theis","Heck","Neyman","Greco","Tippens","Standley","Beers","Dudas","Monday","Rau","Reiff","Adkison","Naval","Fitzhenry","Voight","Crawford","Kinnison","Dorsch","Sharples","Honey","Westlake","Thelen","Hinchman","Sisemore","Clever","Bonier","Blomquist","Asper","Fett","Ciampa","Geraci","Magness","Charpentier","Woodside","Schoch","Pinegar","Huitt","Gilkes","Mcclendon","Crumbley","Dabbs","Loehr","Nadel","Gilbreath","Revis","Galbraith","Nobles","Duvall","Hausner","Flock","Niver","Tester","Norgard","Estabrook","Duffie","Bjelland","Klumpp",
		};

		private static string GetRandomText(List<String> from) {
			return from[rnd.Next(from.Count - 1)];
		}
		public static string GetRandomWord() {
			return GetRandomText(WordList);
		}

		public static string GenerateCompanyName() {
			var n = rnd.Next(2, 4);
			return string.Join(" ",Enumerable.Range(0, n).Select(x => GetRandomWord()))+" "+GetRandomText(CompanySuffix);
		}

		public static Tuple<string, string> GenerateName() {
			return Tuple.Create(GetRandomText(FirstNames), GetRandomText(LastNames));
		}

	}
}


