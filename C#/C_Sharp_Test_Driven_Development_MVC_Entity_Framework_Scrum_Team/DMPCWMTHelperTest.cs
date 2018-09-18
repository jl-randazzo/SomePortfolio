using CWMasterTeacher3;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CWTesting.Tests.CWMasterTeacher3.Services
{
    [TestFixture]
    public class DWMCWMTHelperTest 
    {
        diff_match_patch _dmp;
        DMPCWMTHelper _helper;
        StringComparer _sc;
        string _finished;

        [SetUp]
        public void SetUp()
        {
            _dmp = new diff_match_patch();
            _sc = StringComparer.InvariantCulture;
        }

        [Test]
        public void TestEncodeAndDecode()
        {
            string s1 = "<p>Peter piper picked a peck of pickled pepper &amp; went home.</p>";
            _finished = "&lt;p&gt;Peter piper picked a peck of pickled pepper &amp;amp; went home.&lt;/p&gt;";
            string s1Encoded = HttpUtility.HtmlEncode(s1);
            Assert.IsTrue(_sc.Equals(s1Encoded, _finished));
            Assert.False(_sc.Equals(s1, _finished));

            _finished = "<p>Peter piper picked a peck of pickled pepper & went home.</p>";
            string s1Decoded = HttpUtility.HtmlDecode(s1);
            Assert.IsTrue(_sc.Equals(s1Decoded, _finished));
            Assert.False(_sc.Equals(s1, _finished));
        }

        [Test]
        public void TagParser()
        {
            string s1 = "<p>Peter piper picked a peck of pickled pepper &amp; went home.</p>";
            string s2 = "<h1>Peter parker penned a <p>peck</p> of pickled peppers &amp; came home.</h1>";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1, ref s2);
            Assert.AreEqual(_helper.HtmlTags.Count, 4);
            Assert.AreEqual(_helper.HtmlTags["<p>"], (char)0xE000);
            Assert.AreEqual(_helper.HtmlTags["</p>"], (char)0xE001);
            Assert.AreEqual(_helper.HtmlTags["<h1>"], (char)0xE002);
            Assert.AreEqual(_helper.HtmlTags["</h1>"], (char)0xE003);
            Assert.IsTrue(!s2.Contains("<"));

            //The test below is to demonstrate that the unicode characters are not affected by html encoding
            s1 = HttpUtility.HtmlEncode(s1);
            _finished = (char)0xE000 + "Peter piper picked a peck of pickled pepper &amp;amp; " +
                "went home." + (char)0xE001;
            Assert.IsTrue(_sc.Equals(s1, _finished));
        }

        [Test]
        public void HtmlTagsGetReturnsDeepClone()
        {
            string s1 = "<p>Peter piper picked a peck of pickled pepper &amp; went home.</p>";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1);
            Dictionary<string, char> copiedDictionary = _helper.HtmlTags;
            copiedDictionary["<p>"] = (char)0xE010;
            Assert.AreNotEqual(copiedDictionary["<p>"], _helper.HtmlTags["<p>"]);
        }

        [Test]
        public void ReEncodeDiffs()
        {
            string s1 = (char)0xE000 + "Peter piper picked > a peck of pickled pepper < went home." + (char)0xE001;
            string s2 = (char)0xE002 + "Peter parker penned < a peck of pickled peppers > came home." + (char)0xE003;
            List<Diff> diffList = _dmp.diff_wordMode(s1, s2);
            _helper = new DMPCWMTHelper();
            _helper.ReEncodeDiffs(diffList);

            foreach(var diff in diffList)
            {
                Assert.False(diff.text.Contains("<") || diff.text.Contains(">"));
            }
        }

        [Test]
        public void TestUnicode()
        {
            _helper = new DMPCWMTHelper();
            string s1 = "test" + (char)_helper.UnicodeIndex;
            Assert.AreEqual(s1.Length, 5);
            Assert.AreEqual((char)_helper.UnicodeIndex, (char)0xE000);
        }

        [Test]
        public void Indexer()
        {
            string s1 = "<p>Peter piper <h1>picked &gt; a peck of pickled</h1> pepper &lt; went home.</p>";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1);
            s1 = HttpUtility.HtmlDecode(s1);
            int last = s1.Length - 1;
            int result = _helper.Indexer(s1, x => x >= 0xE003);
            Assert.AreEqual(last, result);
        }

        [Test]
        public void DefineStyleDupDict()
        {
            string s1 = "<p>Peter piper <h1>picked &gt; a peck of pickled</h1> pepper &lt; went home.</p>";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1);
            _helper.DefineStyleDupDict("background:#069edb", "background:#ffa0a0; text-decoration: line-through");
            Dictionary<char, string> testDict = _helper.FinalDict;
            Assert.AreEqual(testDict[(char)0xE000], "<p>");
            Assert.AreEqual(testDict[(char)0xE001], "<h1>");
            Assert.AreEqual(testDict[(char)0xE002], "</h1>");
            Assert.AreEqual(testDict[(char)0xE003], "</p>");
            Assert.AreEqual(_helper.InsStyle, "<font style=\"background:#069edb\">");
            Assert.AreEqual(_helper.DelStyle, "<font style=\"background:#ffa0a0; text-decoration: line-through\">");
        } 

        [Test]
        public void ReplaceStyledTags()
        {
            string s1 = "<p>Pepper piper <i>picked a peck of <strong>pickled peppers and</strong></i> went home.</p>";
            string s2 = "<p>Peter parker <i>picked a peck of <strong>pickled</strong> peppers and came</i> home.</p>";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1, ref s2);
            List<Diff> diffList = _dmp.diff_wordMode(s1, s2);
            _helper.DefineStyleDupDict("background:#069edb", "background:#ffa0a0; text-decoration: line-through");
            _finished = diffList.First().text;
            System.Diagnostics.Debug.WriteLine(_finished);

            _helper.ReplaceStyledTags(diffList.First());
            Assert.AreEqual("<font style=\"background:#ffa0a0; text-decoration: line-through\"><p><font style=\"" +
                "background:#ffa0a0; text-decoration: line-through\">Pepper piper </font>", diffList.First().text);

            _helper.ReplaceStyledTags(diffList.ElementAt(1));
            Assert.AreEqual("<font style=\"background:#069edb\">Peter parker </font>", diffList.ElementAt(1).text);

            _helper.ReplaceStyledTags(diffList.ElementAt(2));

            _helper.ReplaceStyledTags(diffList.ElementAt(3));
            Assert.AreEqual("<font style=\"background:#ffa0a0; text-decoration: line-through\"><strong>pickled " +
                "</strong></font>", diffList.ElementAt(3).text);

            _helper.ReplaceStyledTags(diffList.ElementAt(4));
            Assert.AreEqual("<font style=\"background:#069edb\"><strong>pickled</strong> " +
                "</font>", diffList.ElementAt(4).text);

            Diff newDiff = _helper.ReplaceStyledTags(diffList.ElementAt(5));
            Assert.AreEqual(newDiff.operation, Operation.DELETE);

            _helper.ReplaceStyledTags(newDiff);
            Assert.AreEqual("<font style=\"background:#ffa0a0; text-decoration: line-through\">" +
                "<strong>peppers </strong></font>", newDiff.text);

            _helper.ReplaceStyledTags(diffList.ElementAt(5));
            Assert.AreEqual("<font style=\"background:#069edb\">peppers </font>", diffList.ElementAt(5).text);

            _helper.ReplaceStyledTags(diffList.ElementAt(6));
            _helper.ReplaceStyledTags(diffList.ElementAt(7));
            Assert.AreEqual("<font style=\"background:#069edb\"><i>and came</i> </font>", diffList.ElementAt(7).text);

            _helper.ReplaceStyledTags(diffList.Last());
            Assert.AreEqual("home.</p>", diffList.Last().text);
        }

        [Test]
        public void ProcessBlockCloser()
        {
            string s1 = "<p>The girl went to the store.</p>";
            string s2 = "<p>She went to the store</p>";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1, ref s2);
            List<Diff> diffList = _dmp.diff_wordMode(s1, s2);
            _helper.DefineStyleDupDict("background:#069edb", "background:#ffa0a0; text-decoration: line-through");

            _helper.ReplaceStyledTags(diffList.First());
            Assert.AreEqual("<font style=\"background:#ffa0a0; text-decoration: line-through\">" +
                            "<p><font style=\"background:#ffa0a0; text-decoration: line-through\">The girl </font>",
                            diffList.First().text);

            _helper.ReplaceStyledTags(diffList.ElementAt(1));
            Assert.AreEqual("<font style=\"background:#069edb\">She </font>", diffList.ElementAt(1).text);

            _helper.ReplaceStyledTags(diffList.ElementAt(2));
            Assert.AreEqual("went to the ", diffList.ElementAt(2).text);

            _helper.ReplaceStyledTags(diffList.ElementAt(3));
            Assert.AreEqual("<font style=\"background:#ffa0a0; text-decoration: line-through\">store." +
                "</font>", diffList.ElementAt(3).text);

            _helper.ReplaceStyledTags(diffList.Last());
            Assert.AreEqual("<font style=\"background:#069edb\">store</p></font>", diffList.Last().text);

            s1 = "<p>She went to the store.</p>";
            s2 = "<p>She went to the store</p>";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1, ref s2);
            diffList = _dmp.diff_wordMode(s1, s2);
            _helper.DefineStyleDupDict("background:#069edb", "background:#ffa0a0; text-decoration: line-through");
            _helper.ReplaceStyledTags(diffList.First());
            Assert.AreEqual("<p>She went to the ", diffList.First().text);

            _helper.ReplaceStyledTags(diffList.ElementAt(1));
            Assert.AreEqual("<font style=\"background:#ffa0a0; text-decoration: line-through\">store." +
                "</font>", diffList.ElementAt(1).text);

            _helper.ReplaceStyledTags(diffList.Last());
            Assert.AreEqual("<font style=\"background:#069edb\">store</p></font>", diffList.Last().text);

            s1 = "<p>She went to the store. Then went home.</p>";
            s2 = "<p>She went to the store.</p> <p>Then went home.</p>";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1, ref s2);
            diffList = _dmp.diff_wordMode(s1, s2);
            _helper.DefineStyleDupDict("background:#069edb", "background:#ffa0a0; text-decoration: line-through");

            _helper.ReplaceStyledTags(diffList.First());
            Assert.AreEqual("<p>She went to the ", diffList.First().text);

            _helper.ReplaceStyledTags(diffList.ElementAt(1));
            Assert.AreEqual("<font style=\"background:#ffa0a0; text-decoration: line-through\">store. Then " +
                "</font>", diffList.ElementAt(1).text);

            _helper.ReplaceStyledTags(diffList.ElementAt(2));
            Assert.AreEqual("<font style=\"background:#069edb\">store.</p> " +
                            "<p><font style=\"background:#069edb\">Then </font>", 
                            diffList.ElementAt(2).text);

            _helper.ReplaceStyledTags(diffList.Last());
            Assert.AreEqual("went home.</p>", diffList.Last().text);

            s1 = "<p>Test</p><p>She went to the store.</p>";
            s2 = "<p>She went to the store.</p>";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1, ref s2);
            diffList = _dmp.diff_wordMode(s1, s2);
            _helper.DefineStyleDupDict("background:#069edb", "background:#ffa0a0; text-decoration: line-through");

            _helper.ReplaceStyledTags(diffList.First());
            Assert.AreEqual("<font style=\"background:#ffa0a0; text-decoration: line-through\"><p>" +
                            "<font style=\"background:#ffa0a0; text-decoration: line-through\">Test</p><p>" +
                            "<font style=\"background:#ffa0a0; text-decoration: line-through\">She " +
                            "</font>", diffList.First().text);

            _helper.ReplaceStyledTags(diffList.ElementAt(1));
            Assert.AreEqual("<font style=\"background:#069edb\">She </font>", diffList.ElementAt(1).text);

            _helper.ReplaceStyledTags(diffList.Last());
            Assert.AreEqual("went to the store.</p>", diffList.Last().text);
        }

        [Test]
        public void ActiveTagEquality()
        {
            string s1 = "<p>Pepper piper <i>picked a peck of <strong>pickled peppers and</strong></i> went home.</p>";
            string s2 = "<p>Peter parker <i>picked a peck of <strong>pickled</strong> peppers and</i> came home.</p>";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1, ref s2);
            List<Diff> diffList = _dmp.diff_wordMode(s1, s2);
            _helper.DefineStyleDupDict("background:#069edb", "background:#ffa0a0; text-decoration: line-through");

            _helper.ReplaceStyledTags(diffList.ElementAt(0));
            _helper.ReplaceStyledTags(diffList.ElementAt(3));
            Assert.Contains(new Tuple<char, char>((char)0xE002, (char)0xE003), _helper.DelStyleList);
            Assert.IsTrue(!_helper.ActiveTagEquality());

            _helper.ReplaceStyledTags(diffList.ElementAt(1));
            _helper.ReplaceStyledTags(diffList.ElementAt(2));
            _helper.ReplaceStyledTags(diffList.ElementAt(4));
            Assert.IsTrue(!_helper.ActiveTagEquality());
        }

        [Test]
        public void ReadonlyFunctions()
        {
            _helper = new DMPCWMTHelper();
            Assert.IsTrue(_helper.isBlockOpener("p"));
        }

        [Test]
        public void PrettyDiffFormatter()
        {
            string s1 = "<p>Peter piper picked a peck of.</p>";
            string s2 = "<p>Peter parker penned a peck of.</p>";

            _helper = new DMPCWMTHelper();
            string expected = " <p>Peter <font style=\"background:#ffa0a0; text-decoration: line-through\">piper picked" +
                " </font><font style=\"background:#a0ffa0\">parker penned </font>a peck of.</p> ";
            string actual = _helper.PrettyDiffFormatter(s1, s2);
            Assert.AreEqual(expected, actual);

            _helper = new DMPCWMTHelper();
            expected = " <p>Peter <font style=\"background:#ffa0a0; text-decoration: line-through\"" +
                ">piper picked </font><font style=\"background:#a0ffa0\"" +
                ">parker penned </font>a peck of.</p> ";
            actual = _helper.PrettyDiffFormatter(s1, s2);
            Assert.AreEqual(expected, actual);

            s1 = "<p>These are the teachin<i>g notes for the Box Proble</i>m. Huh? Do we have it now.</p>" +
                "<ol><li>go geoducks!</li><li>Two</li><li> Three sode</li></ol> ";
            s2 = "<p>This is the master narrative for Box Problem</p>";
            _helper = new DMPCWMTHelper();
            expected = " <font style=\"background:#ffa0a0; text-decoration: line-through\"><p><font style=\"background:" +
                "#ffa0a0; text-decoration: line-through\">These are </font><font style=\"background:#a0ffa0\">This is " +
                "</font>the <font style=\"background:#ffa0a0; text-decoration: line-through\">teachin<i>g notes </i>" +
                "</font><font style=\"background:#ffa0a0; text-decoration: line-through\"><i>for </i></font><font style=" +
                "\"background:#ffa0a0; text-decoration: line-through\"><i>the </i></font><font style=\"background:" +
                "#ffa0a0; text-decoration: line-through\"><i>Box </i></font><font style=\"background:#ffa0a0; " +
                "text-decoration: line-through\"><i>Proble</i>m. Huh? Do we have it now.   </font><li><font style=" +
                "\"background:#ffa0a0; text-decoration: line-through\"> go geoducks! </li></font><li><font style=" +
                "\"background:#ffa0a0; text-decoration: line-through\"> Two </li></font><li><font style=\"" +
                "background:#ffa0a0; text-decoration: line-through\">  Three sode </li>   </font><font style=\"" +
                "background:#a0ffa0\">master narrative </font><font style=\"background:#a0ffa0\">for </font>" +
                "<font style=\"background:#a0ffa0\">Box </font><font style=\"background:#a0ffa0\">Problem</p> </font>";
            actual = _helper.PrettyDiffFormatter(s1, s2);
            Assert.AreEqual(expected, actual);

            _helper = new DMPCWMTHelper();
            s1 = "<p>Test <b>sentence a</b>";
            s2 = "<p>Test <b>sentence b</b>";
            expected = " <p>Test <b>sentence <font style=\"background:#ffa0a0; text-decoration: line-through\">a</b>" +
                "</font><font style=\"background:#a0ffa0\"><b>b</b></font>";
            actual = _helper.PrettyDiffFormatter(s1, s2);
            Assert.AreEqual(expected, actual);

            //This tests the split diff concatenator by having inses and dels that need to be added to the list
            s1 = "<p>Pepper piper <i>picked a peck of <strong>pickled peppers and</strong></i> went home.</p>";
            s2 = "<p>Peter parker <i>picked a peck of <strong>pickled</strong> peppers and came</i> home.</p>";
            _helper = new DMPCWMTHelper();
            _finished = _helper.PrettyDiffFormatter(s1, s2);
            expected = " <font style=\"background:#ffa0a0; text-decoration: line-through\"><p><font style=\"" +
                "background:#ffa0a0; text-decoration: line-through\">Pepper piper </font><font style=\"background:" +
                "#a0ffa0\">Peter parker </font><i>picked a peck of <font style=\"background:#ffa0a0; " +
                "text-decoration: line-through\"><strong>pickled </strong></font><font style=\"background:#ffa0a0; " +
                "text-decoration: line-through\"><strong>peppers </strong></font><font style=\"background:#ffa0a0;" +
                " text-decoration: line-through\"><strong>and</strong></i> went </font><font style=\"" +
                "background:#a0ffa0\"><strong>pickled</strong> </font><font style=\"background:#a0ffa0\">peppers " +
                "</font><font style=\"background:#a0ffa0\"><i>and came</i> </font>home.</p> ";
            Assert.AreEqual(expected, _finished);

            //Trying to cover other branch in OtherList method
            s1 = "<i>The test sentence a</i>";
            s2 = "<i>The test </i>sentence<i> a</i>";

            _helper = new DMPCWMTHelper();
            expected = "<i>The test <font style=\"background:#ffa0a0; text-decoration: line-through\">sentence </font>" +
                "<font style=\"background:#ffa0a0; text-decoration: line-through\"><i>a</i></font>" +
                "<font style=\"background:#a0ffa0\"></i>sentence<i> </i></font>" +
                "<font style=\"background:#a0ffa0\"><i>a</i></font>";
            actual = _helper.PrettyDiffFormatter(s1, s2);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void AddSpacesToSpecialTags()
        {
            string s1 = "<p>List:</p><ol><li>Peter</li></ol>";
            string s2 = "<p>List:</p><ol><li>Peter</li></ol>";

            var _helper = new DMPCWMTHelper();
            string expected = " <p>List:</p>  <ol> <li> Peter </li> </ol> ";
            string actual = _helper.PrettyDiffFormatter(s1, s2);
            Assert.AreEqual(expected, actual);

            s1 = "<p>List:</p><ol><li>Peter</li></ol>";
            s2 = "<p>Listy:</p><ol><li>Pepper</li></ol>";
            _helper = new DMPCWMTHelper();
            expected = " <font style=\"background:#ffa0a0; text-decoration: line-through\"><p><font style=\"" +
                "background:#ffa0a0; text-decoration: line-through\">List:</p> </font><font style=\"" +
                "background:#a0ffa0\"><p><font style=\"background:#a0ffa0\">Listy:</p> </font> <ol> <li> <font style=" +
                "\"background:#ffa0a0; text-decoration: line-through\">Peter </font><font style=" +
                "\"background:#a0ffa0\">Pepper </font></li> </ol> ";
            actual = _helper.PrettyDiffFormatter(s1, s2);
            Assert.AreEqual(expected, actual);

            s1 = "<p>List:</p><ul><li>Peter</li></ul>";
            s2 = "<p>Listy:</p><ul><li>Pepper</li></ul>";
            _helper = new DMPCWMTHelper();
            expected = " <font style=\"background:#ffa0a0; text-decoration: line-through\"><p><font style=\"" +
                "background:#ffa0a0; text-decoration: line-through\">List:</p> </font><font style=\"background:" +
                "#a0ffa0\"><p><font style=\"background:#a0ffa0\">Listy:</p> </font> <ul> <li> <font style=\"" +
                "background:#ffa0a0; text-decoration: line-through\">Peter </font><font style=\"background:#a0ffa0\">" +
                "Pepper </font></li> </ul> ";
            actual = _helper.PrettyDiffFormatter(s1, s2);
            Assert.AreEqual(expected, actual); 

            s1 = "<p>Pepper piper <i>picked a of <strong>pickled peppers and</strong></i> went home.</p> ";
            s2 = "<p>Peter parker <i>picked a peck of <strong>pickled</strong> peppers and came</i> home.</p> ";
            _helper = new DMPCWMTHelper();
            actual = _helper.PrettyDiffFormatter(s1, s2);
            Assert.AreEqual("", "");
        }

        [Test]
        public void TagsWithoutClosersDontEndUpInList()
        {
            string s1 = "<test/ >";
            string s2 = "<test/ >";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1, ref s2);
            _helper.DefineStyleDupDict("ins", "del");
            List<Diff> diffList = _dmp.diff_wordMode(s1, s2);

            _helper.ReplaceStyledTags(diffList[0]);
            Assert.IsEmpty(_helper.EqualStyleList);
            Assert.IsEmpty(_helper.DelStyleList);
            Assert.IsEmpty(_helper.InsStyleList);
        }

        [Test]
        public void ListItemProcessing()
        {
            string s1 = "<ul> <li> test item 1 </li> </ul>";
            string s2 = "<ul> <li> test item 2 </li> </ul>";
            _helper = new DMPCWMTHelper();
            _helper.TagParser(ref s1, ref s2);
            List<Diff> listDiff = _dmp.diff_wordMode(s1, s2);
            _helper.DefineStyleDupDict("", "");
            _helper.ReplaceStyledTags(listDiff[0]);
            Assert.IsEmpty(_helper.EqualStyleList);

            _helper.ReplaceStyledTags(listDiff[1]);
            _helper.ReplaceStyledTags(listDiff[2]);
            Assert.DoesNotThrow(() => _helper.ReplaceStyledTags(listDiff[3]));
        }
    }
}
