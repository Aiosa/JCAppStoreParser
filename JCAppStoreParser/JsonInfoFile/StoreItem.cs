using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using Newtonsoft.Json.Linq;

using HtmlAgilityPack;
using System.IO;

namespace JCAppStore_Parser.JsonInfoFile
{
    public class StoreItem : IComparable<StoreItem>
    {
        //correct SDK versions
        public static readonly string[] SDK_VERSIONS = new string[] { "2.1.1", "2.1.2", "2.2.1", "2.2.2", "3.0.1.", "3.0.2", "3.0.3", "3.0.4", "3.0.5" };

        private bool _assignable = false;
        private bool _isNew = false;

        public const string Type = "applet";
        private int _index = 0;
        public string Name { get; set; }
        public string Title { get; set; }
        public List<string> AppletNames { get; set; }
        public string Icon { get; set; }
        public string LatestVersion { get => Versions == null ? null : Versions.Max; }
        public SortedSet<string> Versions { get; set; }
        public Dictionary<string, SortedSet<string>> Builds { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Urls { get; set; }
        public string Usage { get; set; }
        public bool Keys { get; set; }
        public string DefatulSelected { get; set; }
        public string Pgp { get; set; }
        public string SignedBy { get; set; }

        public static StoreItem Empty(Category of)
        {
            return new StoreItem
            {
                _isNew = true,
                _index = of.Last()._index + 1,
                Name = $"NewItem{of.Count+1}",
                Title = null,
                AppletNames = null,
                Icon = null,
                Versions = null,
                Builds = null,
                Author = null,
                Urls = null,
                Description = null,
                Usage = null,
                Keys = false,
                DefatulSelected = null,
                Pgp = null,
                SignedBy = null
            };
        }


        public static StoreItem FromJsonObject(JObject o, int at)
        {
            T FromJArray<T>(JToken from) where T : ICollection<string>, new()
            {
                return from.Children().Aggregate(new T(), 
                    (res, token) => { res.Add(token.ToString()); return res; });
            }
           
            return new StoreItem
            {
                _index = at,
                Name = (string)o[JcappstoreParser.TAG_NAME],
                Title = (string)o[JcappstoreParser.TAG_TITLE],
                AppletNames = FromJArray<List<string>>(o[JcappstoreParser.TAG_APPLET_INSTANCE_NAMES]),
                Icon = (string)o[JcappstoreParser.TAG_ICON],
                Versions = FromJArray<SortedSet<string>>(o[JcappstoreParser.TAG_VERSION]),
                Builds = StoreItemDictionaryConverter<SortedSet<string>>.FromJsonObject(o, JcappstoreParser.TAG_BUILD, x => FromJArray<SortedSet<string>>(x)),
                Author = (string)o[JcappstoreParser.TAG_AUTHOR],
                Urls = StoreItemDictionaryConverter<string>.FromJsonObject(o, JcappstoreParser.TAG_URL, x => (string)x),
                Description = (string)o[JcappstoreParser.TAG_DESC],
                Usage = (string)o[JcappstoreParser.TAG_USAGE],
                Keys = (bool)o[JcappstoreParser.TAG_KEYS],
                DefatulSelected = (string)o[JcappstoreParser.TAG_DEFAULT_SELECTED],
                Pgp = (string)o[JcappstoreParser.TAG_PGP_IDENTIFIER],
                SignedBy = (string)o[JcappstoreParser.TAG_PGP_SIGNER],
            };
        }

        public JObject ToJsonObject()
        {
            JToken ToJArray(ICollection<string> from)
            {
                if (from == null) return new JObject();
                return from.Aggregate(new JArray(),
                    (res, data) => { res.Add(new JValue(data)); return res; });
            }

            JObject DictToJObject<T>(Dictionary<string, T> from, Func<T, JToken> converter)
            {
                if (from == null) return new JObject();
                return from.Aggregate(new JObject(),
                    (res, data) => { res[data.Key] = converter(data.Value); return res; });
            }
            if (Builds == null) throw new Exception("Missing builds: invalid item, cannot save.");
            Versions = new SortedSet<string>(Builds.Keys);

            JObject o = new JObject();
            o[JcappstoreParser.TAG_TYPE] = "applet";
            o[JcappstoreParser.TAG_NAME] = Name;
            o[JcappstoreParser.TAG_TITLE] = Title;
            o[JcappstoreParser.TAG_APPLET_INSTANCE_NAMES] = AppletNames == null || AppletNames.Count == 0 ? null : ToJArray(AppletNames);
            o[JcappstoreParser.TAG_ICON] = Icon == null ? "" : Icon;
            o[JcappstoreParser.TAG_LATEST] = LatestVersion;
            //ignore versions, present only because of value testing, but not updated...
            o[JcappstoreParser.TAG_VERSION] = ToJArray(Versions);
            o[JcappstoreParser.TAG_BUILD] = DictToJObject(Builds, set => ToJArray(set));
            o[JcappstoreParser.TAG_AUTHOR] = Author;
            o[JcappstoreParser.TAG_DESC] = Description;
            o[JcappstoreParser.TAG_URL] = DictToJObject(Urls, url => new JValue(url));
            o[JcappstoreParser.TAG_USAGE] = Usage;
            o[JcappstoreParser.TAG_KEYS] = Keys;
            o[JcappstoreParser.TAG_DEFAULT_SELECTED] = DefatulSelected == null || DefatulSelected.IsEmpty() ? "" : DefatulSelected;
            o[JcappstoreParser.TAG_PGP_IDENTIFIER] = Pgp == null ? "" : Pgp;
            o[JcappstoreParser.TAG_PGP_SIGNER] = SignedBy == null ? "" : SignedBy;
            return o;
        }

        /// <summary>
        /// Syntax validation
        /// (it does not check for sorted builds as the builds 
        /// are always sorted if latest version is set)
        /// </summary>
        public string Validate()
        {
            var builder = new StringBuilder();
            if (!CompulsoryString(Name)) builder.Append($"{Name}::Invalid Name: Empty name identifier for the applet.").Append("\r\n");
            if (!CompulsoryString(Title)) builder.Append($"{Name}::Invalid Title: Empty title.").Append("\r\n") ;
            if (!ValidIcon(Icon)) builder.Append($"{Name}::Invalid icon name: {Icon}").Append("\r\n");
            var error = ValidAppletNames();
            if (error != null) builder.Append($"{Name}::Invalid Applet list: {error}").Append("\r\n") ;
            if (!_isNew && (Versions == null || Versions.Count < 1)) builder.Append($"{Name}::Invalid Version list: must contain at least one version.").Append("\r\n");
            error = ValidBuilds();
            if (error != null) builder.Append($"{Name}::Invalid Builds: {error}").Append("\r\n");
            if (!CompulsoryString(Author)) builder.Append($"{Name}::Missing Author").Append("\r\n");
            error = ValidCompulsoryHtml(Description);
            if (error != null) builder.Append($"{Name}::Invalid Description: {error}").Append("\r\n");
            error = ValidCompulsoryHtml(Usage);
            if (error != null) builder.Append($"{Name}::Invalid Usage: {error}").Append("\r\n");
            if (!ValidDefaultSelected()) builder.Append($"{Name}::Invalid default selected AID: {DefatulSelected}").Append("\r\n");
            if (!NonEmptyContainer(Urls)) builder.Append($"{Name}::Invalid urls: empty field. You should have specify at least repository or official page of the source.").Append("\r\n");
            if (!NonEmptyContainer(Builds)) builder.Append($"{Name}::Invalid builds: empty field. You should have specify at least one build for the item.").Append("\r\n");

            if ((CompulsoryString(Pgp) && !CompulsoryString(SignedBy)) || (!CompulsoryString(Pgp) && CompulsoryString(SignedBy)))
            {
                builder.Append($"{Name}::Invalid pgp data: both PGP and SIGNED_BY must be empty or specified.").Append("\r\n");
            }
            return builder.ToString();
        }
        
        /// <summary>
        /// Validates all fields as if called Validate()
        /// and checks all URLs (expects 200 HTTP response code)
        /// </summary>
        public void ValidateExhaustive(string rootDir, string appletDir, List<AID> aids, Action<string> logger)
        {
            var error = Validate();
            if (error.IsEmpty()) logger(error);

            if (!Directory.Exists(appletDir))
            {
                logger($"ERROR: a folder {appletDir} must exist. Skipping..");
                return;
            }

            if (aids == null || aids.Count < 1) logger("Skipping AID validation. Applets are missing.");
            else
            {
                if (AppletNames != null && AppletNames.Count > 0 && aids.Count != AppletNames.Count)
                    logger($"Invalid applet AIDs: custom values should be defined for each AID in cap binary file ({aids.Count} applet instance(s)).");
                if (DefatulSelected != null && DefatulSelected.Length > 0)
                {
                    var defaultSelected = new AID(DefatulSelected);
                    if (!aids.Any(x => x.Equals(defaultSelected)))
                    {
                        logger($"Invalid default selected applet {DefatulSelected}: .cap file does not contain such AID.");
                    }
                }
            }

            if (Icon != null && Icon.Length > 0 && !File.Exists($@"{rootDir}\Resources\{Icon}")) logger($"Invalid Icon image: not present in /Resources: {Icon}");

            ValidateUrlFields(logger);

            if (Name == null || Name.IsEmpty()) logger("Skipping: no name defined.");
            else ValidateVersions(logger, appletDir);
        }

        /// <summary>
        /// Iset
        /// </summary>
        public StoreItem CopyUpdateable()
        {
            var item = new StoreItem();
            item.Keys = Keys;
            item._assignable = true;
            return item;
        }

        public void Update(StoreItem with)
        {
            if (!with._assignable) throw new Exception("Do not update this object with object not created via CopyUpdateable()");
            if (!Empty(with.Name)) { Name = with.Name; with.Name = null; }
            if (!Empty(with.Title)) { Title = with.Title; with.Title = null; }
            if (!Empty(with.AppletNames)) { AppletNames = with.AppletNames; with.AppletNames = null; }
            if (!Empty(with.Icon)) { Icon = with.Icon; with.Icon = null; }
            if (!Empty(with.Versions)) { Versions = with.Versions; with.Versions = null; }
            if (!Empty(with.Builds)) { Builds = with.Builds; with.Builds = null; }
            if (!Empty(with.Author)) { Author = with.Author; with.Author = null; }
            if (!Empty(with.Description)) { Description = with.Description; with.Description = null; }
            if (!Empty(with.Urls)) { Urls = with.Urls; with.Urls = null; }
            if (!Empty(with.Usage)) { Usage = with.Usage; with.Usage = null; }
            Keys = with.Keys;
            if (!Empty(with.DefatulSelected)) { DefatulSelected = with.DefatulSelected; with.DefatulSelected = null; }
            if (!Empty(with.Pgp)) { Pgp = with.Pgp; with.Pgp = null; }
            if (!Empty(with.SignedBy)) { SignedBy = with.SignedBy; with.SignedBy = null; }

            bool Empty<T>(IEnumerable<T> item) => item == null || item.Count() < 1;
        }

        public override string ToString()
        {
            return Title;
        }

        public override bool Equals(object y)
        {
            if (y == null || !(y is StoreItem))
            {
                return false;
            }
            return Name.Equals(((StoreItem)y).Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public int CompareTo(StoreItem other)
        {
            return _index.CompareTo(other._index);
        }

        //////////////////////////////////////////
        /// COMPLEX VALUES STRING REPRESENTATIONS
        //////////////////////////////////////////
        
        public string GetValuesNotNull()
        {
            var builder = new StringBuilder();
            builder.Append(GetValueOrEmptyString("Applet: ", Title));
            builder.Append(GetValueOrEmptyString("Identifier Name: ", Name));
            builder.Append(GetValueOrEmptyString("Instances: ", GetAppletNames()));
            builder.Append(GetValueOrEmptyString("Image: ", Icon));
            builder.Append(GetValueOrEmptyString("LatestVersion: ", LatestVersion));
            if (Builds != null)
            {
                builder.Append("Builds: \r\n");
                foreach (var build in Builds)
                {
                    builder.Append(GetValueOrEmptyString($"  {build.Key} ", FieldUtils.GetValues(build.Value)));
                }
            }
            builder.Append(GetValueOrEmptyString("Author: ", Author));
            builder.Append(GetValueOrEmptyString("Description: ", Description, str => ((string)str).Cut(130)));
            builder.Append(GetValueOrEmptyString("Usage: ", Usage, str => ((string)str).Cut(140)));
            if (Urls != null)
            {
                builder.Append("Web links: \r\n");
                foreach (var link in Urls)
                {
                    builder.Append(GetValueOrEmptyString($"  {link.Key} ", link.Value));
                }
            }
            builder.Append("Keys: ").Append(Keys).Append("\r\n");
            builder.Append(GetValueOrEmptyString("DefaultSelected: ", DefatulSelected));
            builder.Append(GetValueOrEmptyString("PGP file: ", Pgp));
            builder.Append(GetValueOrEmptyString("PGP fingerprint: ", SignedBy));
            return builder.ToString();
        }

        public string GetValues()
        {
            var builder = new StringBuilder();
            builder.Append("Applet: ").Append(Title).Append("\r\n")
              .Append("Identifier Name: ").Append(Name).Append("\r\n")
              .Append("Instances: ").Append(GetAppletNames()).Append("\r\n")
              .Append("Image: ").Append(Icon).Append("\r\n")
              .Append("LatestVersion: ").Append(LatestVersion).Append("\r\n")
              .Append("Builds: \r\n");
            if (Builds != null)
            {
                foreach (var build in Builds)
                {
                    builder.Append(GetValueOrEmptyString($"  {build.Key} ", FieldUtils.GetValues(build.Value)));
                }
            }
            else
            {
                builder.Append("  [missing]").Append("\r\n");
            }
            builder.Append("Author: ").Append(Author).Append("\r\n")
              .Append("Description: ").Append(Description == null ? "No description." : Description.Cut(130)).Append("\r\n")
              .Append("Usage: ").Append(Usage == null ? "No usage." : Usage.Cut(140)).Append("\r\n")
              .Append("Web links: \r\n");
            if (Urls != null)
            {
                foreach (var link in Urls)
                {
                    builder.Append(GetValueOrEmptyString($"  {link.Key} ", link.Value));
                }
            }
            else
            {
                builder.Append("  [empty]").Append("\r\n");
            }
            builder.Append("Keys: ").Append(Keys).Append("\r\n")
              .Append("DefaultSelected: ").Append(DefatulSelected).Append("\r\n")
              .Append("PGP file: ").Append(Pgp).Append("\r\n")
              .Append("PGP fingerprint: ").Append(SignedBy).Append("\r\n");
            return builder.ToString();
        }

        private string GetValueOrEmptyString(string prefix, object attribute, Func<object, string> transformer = null)
        {
            if (attribute == null) return "";
            if (transformer == null) return $"{prefix}{attribute}\r\n";
            return $"{prefix}{transformer(attribute)}\r\n";
        }

        private string GetAppletNames(string emptyMessage = null)
        {
            if (AppletNames == null || AppletNames.Count < 1) return emptyMessage;
            StringBuilder builder = new StringBuilder("\r\n");
            if (AppletNames[0].Equals("0x"))
            {
                for (int i = 1; i < AppletNames.Count;)
                {
                    builder.Append(' ').Append(AppletNames[i++]).Append(", AID:").Append(AppletNames[i++]).Append("\r\n");
                }
                return builder.ToString();
            }
            foreach (var name in AppletNames)
            {
                builder.Append(' ').Append(name).Append("\r\n");
            }
            return builder.ToString();
        }

        //////////////////////////////////
        /// SYNTAX VALIDATION METHODS
        //////////////////////////////////

        public bool NonEmptyContainer<T>(IEnumerable<T> e)
        {
            if (e == null || e.Count() < 1) return false;
            return true;
        }

        private bool CompulsoryString(string value)
        {
            return !(value == null || value.IsEmpty());
        }

        private bool ValidIcon(string value)
        {
            return !CompulsoryString(value) || value.EndsWith(".png") || value.EndsWith(".jpg") || value.EndsWith(".jpeg");
        }

        private bool ValidDefaultSelected()
        {
            return !CompulsoryString(DefatulSelected) || AID.Valid(DefatulSelected);
        }

        private string ValidAppletNames()
        {
            if (AppletNames == null) return null;
            if (AppletNames.Count < 1 || !AppletNames[0].Equals("0x"))
            {
                return null;
            }
            if (AppletNames.Count % 2 == 0) return "Applet list should either contain custom names only, or both custom name and AID for all the applets.";
            var builder = new StringBuilder();
            for (int i = 1; i < AppletNames.Count; i += 2)
            {
                if (AppletNames[i].IsEmpty()) builder.Append($"  Applet name at index {i} must not be missing.\r\n");
                if (!AID.Valid(AppletNames[i + 1])) builder.Append($"  Invalid custom applet AID: {AppletNames[i + 1]}\r\n");
            }
            var result = builder.ToString();
            return result.IsEmpty() ? null : result;
        }

        private string ValidBuilds()
        {
            var builder = new StringBuilder();
            if (Builds == null || Builds.Count < 1) return "Applet must specify at least one build.";
            foreach(var build in Builds)
            {
                if (build.Value == null || build.Value.Count < 1 || !build.Value.All(x => SDK_VERSIONS.Contains(x)))
                {
                    builder.Append($"  Incorrect sdk value for version {build.Key}.\r\n");
                }

            }
            var result = builder.ToString();
            return result.IsEmpty() ? null : result;
        }

        private string ValidCompulsoryHtml(string value)
        {
            if (!CompulsoryString(value)) return "The field is empty.";
            var doc = new HtmlDocument();
            doc.LoadHtml($"<html><head></head><body>{value}</body></html>");

            if (doc.ParseErrors.Count() > 0)
            {
                return doc.ParseErrors.Aggregate("\r\n", (res, error) => $"{res}\r\n[{error.Line}]: {error.Reason}\r\n");
            }
            return null;
        }

        //////////////////////////////////
        /// SEMANTICS VALIDATION METHODS
        //////////////////////////////////

        /// <summary>
        /// Validates URLs by sending HTTP request
        /// </summary>
        private void ValidateUrlFields(Action<string> logger)
        {
            using (var client = new Web())
            {
                foreach (var keyPair in Urls)
                {
                    try
                    {
                        int statusCode = client.GetStatusCodeOf(keyPair.Value);
                        if (statusCode != 200) logger($"Url {keyPair.Value} returned {statusCode}");
                    }
                    catch (NotSupportedException e)
                    {
                        logger($"Unable to verify address {keyPair.Value}: {e.Message}");
                    }
                }
            }
        }

        private void ValidateVersions(Action<string> logger, string rootDir)
        {
            foreach (var version in Builds)
            {
                foreach(var sdk in version.Value)
                {
                    string file = $@"{rootDir}\{Name}_v{version.Key}_sdk{sdk}.cap";
                    if (!File.Exists(file)) logger($"Version {version} specified with {sdk} SDK, but missing file: {file}");
                }
            }
        }

        private class Web : WebClient
        {
            private WebResponse response = null;
            public int StatusCode
            {
                get => (response != null && response is HttpWebResponse) ?
                         (int)(response as HttpWebResponse).StatusCode : 200;
            }

            public int GetStatusCodeOf(string url)
            {
                if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri uri))
                {
                    GetWebResponse(GetWebRequest(uri));
                    return StatusCode;
                }
                return -1;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request.Method == "GET")
                {
                    request.Method = "HEAD";
                }
                request.Timeout = 5000;
                return request;
            }

            protected override WebResponse GetWebResponse(WebRequest request)
            {
                try
                {
                    response = base.GetWebResponse(request);
                }
                catch (WebException e)
                {
                    if (response == null) response = e.Response;
                }
                return response;
            }
        }
    }
}
