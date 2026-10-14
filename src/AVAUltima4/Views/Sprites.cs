using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AVAUltima4.Views;

/// <summary>
/// The game's pixel art. People, creatures, places and furniture are 16×16 drawings, one letter per
/// pixel: 'a' is the tint (each figure's own colour), 'b' a darker and 'c' a lighter shade of it;
/// the other letters are fixed colours (see <see cref="Fixed"/>); '.' is clear. The open land's
/// ground (grass, forest, hills, water...) is painted by <see cref="Ground"/> instead. Every picture
/// is turned into a small bitmap once and drawn scaled up, pixel for pixel. All original art.
/// </summary>
public static class Sprites
{
    public static readonly Dictionary<char, uint> Fixed = new()
    {
        ['k'] = 0xFF141218, ['w'] = 0xFFF0EEE6, ['r'] = 0xFFD03030, ['y'] = 0xFFF0D040, ['s'] = 0xFFE0B090,
        ['g'] = 0xFF8A8A90, ['G'] = 0xFF4E4E58, ['n'] = 0xFF8A5A30, ['N'] = 0xFF4A3020, ['e'] = 0xFFFF4020,
        ['l'] = 0xFFFFF4B0, ['o'] = 0xFFFF8020, ['u'] = 0xFF3060C0, ['h'] = 0xFF4E9A3A, ['H'] = 0xFF2A5A26,
        ['j'] = 0xFF8CC860, ['m'] = 0xFF6E6A64, ['M'] = 0xFFB8B4AC, ['x'] = 0xFF3E2A1C, ['z'] = 0xFF2A64A8,
        ['Z'] = 0xFF7AB8E8,
    };

    public static readonly Dictionary<string, string[]> All = new()
    {
        ["fighter"] =
        [
            "................",
            "......GGGG......",
            ".....GggggG.....",
            ".....GgkgkG.....",
            ".....GssssG.....",
            "......ssss....w.",
            "...aaaaaaaaa..w.",
            "..GaaacccaaaG.w.",
            ".GgGaaccaaaGs.w.",
            ".GggGaaaaaa.snn.",
            ".GggGaaaaaa..k..",
            "..GG.bbbbbb.....",
            ".....aa..aa.....",
            ".....GG..GG.....",
            "....GGG..GGG....",
            "................",
        ],
        ["mage"] =
        [
            ".......a........",
            "......aaa.....l.",
            ".....aacaa...lyl",
            "....aaaaaaa...n.",
            "...aaaaaaaaa..n.",
            ".....ssss.....n.",
            ".....kssk.....n.",
            "......ss......n.",
            "....aaaaaa...sn.",
            "...aaacaaaasssn.",
            "...aaacaaaa...n.",
            "...aaaaaaaa...n.",
            "...aaaaaaaa...n.",
            "..aaaaaaaaaa..n.",
            "..nnn....nnn..n.",
            "................",
        ],
        ["bard"] =
        [
            "................",
            ".......aaa..r...",
            "......aaaaar....",
            ".....aaaaaa.....",
            "......ssss......",
            "......skks......",
            "......ssss......",
            "....aaaaaaa.....",
            "...aaacaaaaann..",
            "..s.aacaaanyyn..",
            "..s.aaaaaasnnn..",
            "....aaaaaa..n...",
            "....bbbbbb......",
            ".....aa.aa......",
            ".....nn.nn......",
            "....nnn.nnn.....",
        ],
        ["druid"] =
        [
            "................",
            "......aaaa......",
            ".....aaaaaa.....",
            ".....assssa.....",
            ".....askska.....",
            "......ssss......",
            "....aaayaaaa..g.",
            "...aaayyyaaaagg.",
            "..saaaayaaaa.gg.",
            "..s.aaaaaaaa..n.",
            "....aaaaaaaa..n.",
            "....aaaaaaaa....",
            "....aaaaaaaa....",
            "...aaaaaaaaaa...",
            "...nnn....nnn...",
            "................",
        ],
        ["tinker"] =
        [
            "................",
            "......nnnn......",
            ".....nnnnnn.....",
            ".....ssssss.....",
            ".....skssks.....",
            ".....ssssss..gg.",
            "......ssss..ggg.",
            "....aaaaaaaa.n..",
            "...aannnnnnaan..",
            "..sa.nnnnnn.as..",
            "..s..nnnnnn.....",
            "....aaaaaaaa....",
            "....aaa..aaa....",
            "....aa....aa....",
            "...NNN....NNN...",
            "................",
        ],
        ["paladin"] =
        [
            "......aaaa......",
            ".....aaaaaa.....",
            ".....akkkka.....",
            ".....akeeka.....",
            ".....aaaaaa..c..",
            "...aaaaaaaaaac..",
            "..aacaaaaaacaac.",
            "..aacaaaaaacabc.",
            "..aa.aaaaaa.ab..",
            "..aa.aaaaaa.a...",
            "..cc.aaaaaa.....",
            ".....bbbbbb.....",
            ".....aa..aa.....",
            ".....aa..aa.....",
            "....aaa..aaa....",
            "................",
        ],
        ["ranger"] =
        [
            "................",
            "......aaaa......",
            ".....aaaaaa..n..",
            ".....assssa..nw.",
            ".....askska..n.w",
            "......ssss...n.w",
            "....aaaaaaaa.n.w",
            "...aaaaaaaasssnw",
            "..saannnnaaa.n.w",
            "..s.aaaaaaa..n.w",
            "....aaaaaaa..n.w",
            "....aaaaaaa..nw.",
            "....GGG.GGG..n..",
            "....GG...GG.....",
            "...nnn...nnn....",
            "................",
        ],
        ["shepherd"] =
        [
            "..........nnn...",
            ".........n...n..",
            "......aaaa...n..",
            ".....aaaaaa..n..",
            ".....assssa..n..",
            ".....askska..n..",
            "......ssss...n..",
            "....aaaaaaaa.n..",
            "...aaaaaaaaasn..",
            "..s.aaaaaaaa.n..",
            "....aaaaaaaa.n..",
            "....aaaaaaaa.n..",
            "....bbbbbbbb.n..",
            ".....ss..ss..n..",
            "....nnn..nnn....",
            "................",
        ],
        ["man"] =
        [
            "................",
            "......NNNN......",
            ".....NNNNNN.....",
            ".....ssssss.....",
            ".....skssks.....",
            ".....ssssss.....",
            "......ssss......",
            "....aaaaaaaa....",
            "...aaaaaaaaaa...",
            "..s.aaaaaaaa.s..",
            "..s.aaaaaaaa.s..",
            "....nnnnnnnn....",
            "....GGG..GGG....",
            "....GG....GG....",
            "...NNN....NNN...",
            "................",
        ],
        ["woman"] =
        [
            "................",
            "......nnnn......",
            ".....nnnnnn.....",
            "....nnssssnn....",
            "....nskssksn....",
            "....nssssssn....",
            "....n.ssss.n....",
            "....aaaaaaaa....",
            "...aaaaccaaaa...",
            "..s.aaaaaaaa.s..",
            "..s.aaaaaaaa.s..",
            "...aaaaaaaaaa...",
            "...aaaaaaaaaa...",
            "..aaaaaaaaaaaa..",
            "...NN......NN...",
            "................",
        ],
        ["child"] =
        [
            "................",
            "................",
            "................",
            "................",
            "......nnnn......",
            ".....nssssn.....",
            ".....skssks.....",
            ".....ssssss.....",
            "......aaaa......",
            ".....aaaaaa.....",
            "....s.aaaa.s....",
            "......aaaa......",
            "......bbbb......",
            "......s..s......",
            ".....NN..NN.....",
            "................",
        ],
        ["guard"] =
        [
            "................",
            "....kkkkkkkk....",
            ".....kkkkkk.....",
            ".....ssssss.....",
            ".....skssks.....",
            "......ssss......",
            ".g..aaaaaaaa..g.",
            ".g.aayaaaayaa.g.",
            ".gsaaaaaaaaaasg.",
            ".k..aaaaaaaa..k.",
            "....aaaaaaaa....",
            "....aaaaaaaa....",
            "....aaGG.GGa....",
            "....GG....GG....",
            "...kkk....kkk...",
            "................",
        ],
        ["beggar"] =
        [
            "................",
            "................",
            ".....gggg.......",
            "....ggssgg......",
            "....gskskg......",
            "....ggssgg......",
            "...aaaaaaa......",
            "..aaabaaaaa.....",
            "..aabaaaaaaa....",
            ".saaaaaaabaa....",
            ".s.aabaaaaaaa...",
            "w..aaaaabaaaa...",
            "ww.aaaaaaaaaa...",
            "..baaaaaaaaab...",
            "..NN.......NN...",
            "................",
        ],
        ["sage"] =
        [
            ".......aa.......",
            "......aaaa......",
            ".....aaaaaa.....",
            "....aaaaaaaa....",
            ".....ssssss.....",
            ".....skssks.....",
            ".....wwwwww.....",
            "......wwww....n.",
            "....aaawwaaa..n.",
            "...aaaawwaaaasn.",
            "..s.aaaaaaaa..n.",
            "....aaaaaaaa..n.",
            "....aaaaaaaa..n.",
            "...aaaaaaaaaa.n.",
            "...bbbbbbbbbb...",
            "................",
        ],
        ["noble"] =
        [
            "................",
            ".....aaaaaa.....",
            "....aayaaaaa....",
            ".....ssssss.....",
            ".....skssks.....",
            ".....ssssss.....",
            "......ssss......",
            "....aaaaaaaa....",
            "...aayaaaayaa...",
            "..s.aaayyaaa.s..",
            "..s.aaaaaaaa.s..",
            "....aaaaaaaa....",
            "....wwww.www....",
            "....ww....ww....",
            "...kkk....kkk...",
            "................",
        ],
        ["queen"] =
        [
            "......y.y.y.....",
            "......yyyyy.....",
            ".....nnnnnnn....",
            "....nnssssssn...",
            "....nsksssksn...",
            "....nnssssssn...",
            "....n..sss..n...",
            "....aaaaaaaa....",
            "...aacaaaacaa...",
            "..s.aaayyaaa.s..",
            "..s.aaaaaaaa.s..",
            "...aaaaaaaaaa...",
            "...aaaaaaaaaa...",
            "..aaaaaaaaaaaa..",
            ".bbbbbbbbbbbbbb.",
            "................",
        ],
        ["merchant"] =
        [
            "................",
            "......aaaa......",
            ".....aaaaaa.....",
            ".....ssssss.....",
            ".....skssks.....",
            ".....ssssss.....",
            "......ssss......",
            "...aaaaaaaaaa...",
            "..aaawwwwwwaaa..",
            "..s.wwwwwwww.s..",
            "..s.wwwwwwww.s..",
            "....wwwwwwww....",
            "....aaaaaaaa....",
            "....aa....aa....",
            "...NNN....NNN...",
            "................",
        ],
        ["sheep"] =
        [
            "................",
            "................",
            "................",
            "................",
            "................",
            ".....wwwwww.....",
            "...wwwwwwwwww...",
            "..wwwwwwwwwwwkk.",
            "..wwwwwwwwwwkwk.",
            "..wwwwwwwwwwkkk.",
            "...wwwwwwwwww...",
            "....k.k..k.k....",
            "....k.k..k.k....",
            "................",
            "................",
            "................",
        ],
        ["pilgrim"] =
        [
            "................",
            "......aaaa......",
            ".....aaaaaa.....",
            "....aaasssaa.n..",
            "....aaskskaa.n..",
            "....aassssaa.n..",
            ".....aaaaaa..n..",
            "....aaaaaaaasn..",
            "...aaaaaaaaa.n..",
            "...aaaaaaaaa.n..",
            "...aaaaaaaaa.n..",
            "...aaaaaaaaa.n..",
            "...aaaaaaaaa.n..",
            "...bbbbbbbbb.n..",
            "....NN...NN.....",
            "................",
        ],
        ["peddler"] =
        [
            "................",
            "......nnnn......",
            ".....nnnnnn.....",
            ".....ssssss..nn.",
            ".....skssks.nnnn",
            ".....ssssss.nyyn",
            "......ssss..nnnn",
            "....aaaaaaaannnn",
            "...aaaaaaaaannn.",
            "..s.aaaaaaaa.n..",
            "..s.aaaaaaaa....",
            "....aaaaaaaa....",
            "....GGG..GGG....",
            "....GG....GG....",
            "...NNN....NNN...",
            "................",
        ],
        ["rat"] =
        [
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            ".......aaaa.....",
            ".....aaaaaaaa...",
            "...aaaaaaaaaaa..",
            "..kaaaaaaaaacca.",
            ".aeaaaaaaaaaaa.g",
            ".aaaaaaaaaaaa..g",
            "..ww.bb..bb...g.",
            "........gggggg..",
            "................",
            "................",
        ],
        ["snake"] =
        [
            "................",
            ".........aaaa...",
            "........aaaaaa..",
            "........ayaaya..",
            "........aaaaaa..",
            ".........arra...",
            "..........aa....",
            ".........aa.....",
            "....aaaaaa......",
            "...aa...........",
            "...aa....aaaa...",
            "....aaaaaa..aa..",
            "............aa..",
            "....aaaaaaaaa...",
            "...aa...........",
            "....aaa.........",
        ],
        ["bat"] =
        [
            "................",
            "................",
            "................",
            "................",
            "a..............a",
            "aa....a..a....aa",
            "aaa...aaaa...aaa",
            "aaaa.aeaaea.aaaa",
            "aaaaaaaaaaaaaaaa",
            ".aaaaaawwaaaaaa.",
            "..aaa.aaaa.aaa..",
            "...a...aa...a...",
            "................",
            "................",
            "................",
            "................",
        ],
        ["wolf"] =
        [
            "................",
            "................",
            "................",
            "..aa............",
            ".aaaa...........",
            "aeaaaa..........",
            "aaaaaaaaaaaaa...",
            ".oo.aaaaaaaaaa..",
            ".oo.aaaaaaaaaaa.",
            "..o.aaaaaaaaa.a.",
            "....aa.aa.aa.a..",
            "....aa.aa.aa....",
            "....a..a..a.....",
            "................",
            "................",
            "................",
        ],
        ["spider"] =
        [
            "................",
            "................",
            "................",
            "..a..........a..",
            "...a........a...",
            "a...a.aaaa.a...a",
            ".a...aaaaaa...a.",
            "..aaaaeaaeaaaa..",
            "....aaaaaaaa....",
            "..aaaaaaaaaaaa..",
            ".a...aaaaaa...a.",
            "a...a.aaaa.a...a",
            "...a........a...",
            "..a..........a..",
            "................",
            "................",
        ],
        ["slime"] =
        [
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "......aaaa......",
            "....aaccaaaa....",
            "...accaaaaaaa...",
            "..aacaakaakaaa..",
            "..aaaaaaaaaaaa..",
            ".aaaaaaaaaaaaaa.",
            ".aaaaaaaaaaaaaa.",
            "aabbbbbbbbbbbbaa",
            "................",
            "................",
        ],
        ["wisp"] =
        [
            "................",
            "................",
            "......cccc......",
            "....cccccccc....",
            "...ccaaaaaacc...",
            "...caaawwaaac...",
            "..ccaawwwwaacc..",
            "..caawwwwwwaac..",
            "..caawwwwwwaac..",
            "..ccaawwwwaacc..",
            "...caaawwaaac...",
            "...ccaaaaaacc...",
            "....cccccccc....",
            "......cccc......",
            "................",
            "................",
        ],
        ["bear"] =
        [
            "................",
            "................",
            "................",
            "...aa...........",
            "..aaaa..aa......",
            ".aaaaaaaaaa.....",
            "aeaaaaaaaaaaaa..",
            "aaaaaaaaaaaaaaa.",
            "kaaaaaaaaaaaaaaa",
            ".www.aaaaaaaaaaa",
            ".....aaaaaaaaaa.",
            ".....aaa..aaa...",
            ".....aaa..aaa...",
            "....bbb..bbb....",
            "................",
            "................",
        ],
        ["brigand"] =
        [
            "................",
            "......nnnn......",
            ".....nnnnnn.....",
            ".....ssssss.....",
            ".....akkkka.....",
            ".....ssssss.....",
            "......ssss......",
            "....aaaaaaaa..g.",
            "...aaaaaaaaaa.g.",
            "..saaGGGGGaaa.g.",
            "..s.aaaaaaaa.sk.",
            "....aaaaaaaa....",
            "....GGG..GGG....",
            "....GG....GG....",
            "...nnn....nnn...",
            "................",
        ],
        ["goblin"] =
        [
            "................",
            "................",
            "..a..........a..",
            "..aa..aaaa..aa..",
            "...aaaaaaaaaa...",
            "....aeaaaaea....",
            "....aaaaaaaa....",
            ".....awwwwa.....",
            "......aaaa......",
            "....aannnnaa....",
            "...a.annnna.a...",
            "...g.annnna.g...",
            ".....aa..aa.....",
            ".....aa..aa.....",
            "....bbb..bbb....",
            "................",
        ],
        ["archer"] =
        [
            "................",
            "......aaaa......",
            ".....aaaaaa..n..",
            ".....assssa..nw.",
            ".....askska..n.w",
            "......ssss...n.w",
            "....aaaaaaaa.n.w",
            "...aaaaaaaasssnw",
            "..saannnnaaa.n.w",
            "..s.aaaaaaa..n.w",
            "....aaaaaaa..n.w",
            "....aaaaaaa..nw.",
            "....GGG.GGG..n..",
            "....GG...GG.....",
            "...nnn...nnn....",
            "................",
        ],
        ["skeleton"] =
        [
            "................",
            "......aaaa......",
            ".....aaaaaa.....",
            ".....akaaka.....",
            ".....aaaaaa.....",
            "......akka......",
            ".......aa.......",
            "....aaaaaaaa....",
            "...a.a.aa.a.a...",
            "...a.aaaaaa.a...",
            "...a..a.aa.a.a..",
            ".......aa.......",
            "......a..a......",
            "......a..a......",
            ".....aa..aa.....",
            "................",
        ],
        ["orc"] =
        [
            "................",
            ".....aaaaaa.....",
            "....aaaaaaaa....",
            "....aeaaaaea....",
            "....aaaaaaaa....",
            "....awaaaawa....",
            ".....aaaaaa..GG.",
            "...GGGGGGGGGGGG.",
            "..aaGGGGGGGGa.n.",
            "..aa.nnnnnn.a.n.",
            "..aa.nnnnnn.a.n.",
            "..ss.aaaaaa.s.n.",
            ".....aa..aa.....",
            ".....aa..aa.....",
            "....GGG..GGG....",
            "................",
        ],
        ["pirate"] =
        [
            "................",
            "......rrrr......",
            ".....rrrrrrr....",
            ".....ssssss.r...",
            ".....kkssks.....",
            ".....ssssss.....",
            "......nnnn...g..",
            "....aaaaaaaa.g..",
            "...awawawawa.g..",
            "..s.aaaaaaaa.n..",
            "..s.awawawaas...",
            "....aaaaaaaa....",
            "....GGG..GGG....",
            "....GG....n.....",
            "...NNN....n.....",
            "................",
        ],
        ["seaserpent"] =
        [
            "................",
            "........aaaa....",
            ".......aaaaaa...",
            ".......aayaaaa..",
            ".......aaaaaaww.",
            "........aaaarr..",
            "........aaa.....",
            ".......aaa......",
            "......aaa.......",
            "...aa.aaa..aa...",
            "..aaaaaaa.aaaa..",
            ".aaa..aaaaa..aa.",
            "aaa....aaa....aa",
            "ZZZZZZZZZZZZZZZZ",
            ".ZZ..ZZ..ZZ..ZZ.",
            "................",
        ],
        ["ghoul"] =
        [
            "................",
            "................",
            ".......aaaa.....",
            "......aaaaaa....",
            "......aeaeaa....",
            "......aakkaa....",
            "....aaaaaaaa....",
            "...aaaaaaaaaaa..",
            "..aa.aaaaaa..aa.",
            "..w..aaaaaa...w.",
            ".w...aaaaaa....w",
            ".....bbbbbb.....",
            ".....aa..aa.....",
            "....aa....aa....",
            "...aa......aa...",
            "................",
        ],
        ["troll"] =
        [
            "................",
            ".....aaaaaa.....",
            "....aaaaaaaa..n.",
            "...aeaaaaaeaa.nn",
            "...aaaaaaaaaa.nn",
            "....awwwwwwa..n.",
            ".....aaaaaa...n.",
            "..aaannnnnnaaan.",
            ".aaaannnnnnaaaa.",
            ".aa.annnnnna.ss.",
            ".aa.annnnnna....",
            ".ss.aaaaaaaa....",
            "....aaa..aaa....",
            "....aaa..aaa....",
            "...bbbb..bbbb...",
            "................",
        ],
        ["shaman"] =
        [
            "..o..........o..",
            ".ooo........ooo.",
            "..s....aa....s..",
            "..s...aaaa...s..",
            "..s..aaaaaa..s..",
            "..sa.aakkaa.as..",
            "...aaakllkaaa...",
            "....aakkkkaa....",
            "....aaaaaaaa....",
            "....aaayyaaa....",
            "....aaaaaaaa....",
            "...aaaaaaaaaa...",
            "...aaaaaaaaaa...",
            "..aaaaaaaaaaaa..",
            "..bbbbbbbbbbbb..",
            "................",
        ],
        ["wraith"] =
        [
            "................",
            "......aaaa......",
            ".....aaaaaa.....",
            "....aakkkkaa....",
            "....akekekka....",
            "....akkkkkka....",
            "...aaakkkkaaa...",
            "..aaaaaaaaaaaa..",
            ".cc.aaaaaaaa.cc.",
            ".c..aaaaaaaa..c.",
            "....aaaaaaaa....",
            "....aaaaaaaa....",
            ".....aaaaaa.....",
            ".....a.aa.a.....",
            "....a...a..a....",
            "................",
        ],
        ["golem"] =
        [
            "................",
            ".....aaaaaa.....",
            ".....aeaaea.....",
            ".....aaaaaa.....",
            "...aaaaaaaaaa...",
            "..aaaaaaaaaaaa..",
            "..aabaaaaaabaa..",
            "..aa.aaaaaa.aa..",
            "..aa.aaaaaa.aa..",
            ".aaa.aabbaa.aaa.",
            ".aaa.aaaaaa.aaa.",
            ".....aa..aa.....",
            ".....aa..aa.....",
            "....aaa..aaa....",
            "....bbb..bbb....",
            "................",
        ],
        ["drake"] =
        [
            "................",
            "..........aaa...",
            "..c......aaaaa..",
            "..cc....aayaaa..",
            "..ccc...aaaaaee.",
            "..cccc.aaaaa....",
            "...ccccaaaa.....",
            "....aaaaaaaa....",
            "..aaaaaaaaaaa...",
            ".aaaaaaaaaaaaa..",
            "aa..aaaaaaa.aa..",
            "a...aa...aa..aa.",
            "....aa...aa...a.",
            "...bbb..bbb.....",
            "................",
            "................",
        ],
        ["knight"] =
        [
            "......aaaa......",
            ".....aaaaaa.....",
            ".....akkkka.....",
            ".....akeeka.....",
            ".....aaaaaa..c..",
            "...aaaaaaaaaac..",
            "..aacaaaaaacaac.",
            "..aacaaaaaacabc.",
            "..aa.aaaaaa.ab..",
            "..aa.aaaaaa.a...",
            "..cc.aaaaaa.....",
            ".....bbbbbb.....",
            ".....aa..aa.....",
            ".....aa..aa.....",
            "....aaa..aaa....",
            "................",
        ],
        ["lich"] =
        [
            ".....y.y.y......",
            ".....yyyyy......",
            ".....wwwww......",
            "....wkwwwkw.....",
            "....wwwkwww.....",
            ".....wkwkw......",
            "....aaaaaaa.....",
            "...aaaaaaaaa.e..",
            "..w.aaaaaaa..n..",
            ".w..aaaaaaa.wn..",
            "....aaaaaaa..n..",
            "....aaaaaaa..n..",
            "...aaaaaaaaa.n..",
            "..aaaaaaaaaaa...",
            "..bbbbbbbbbbb...",
            "................",
        ],
        ["town"] =
        [
            "................",
            "..rr......rr....",
            ".rrrr....rrrr...",
            "rrrrrr..rrrrrr..",
            ".gwwg....gwwg...",
            ".gkwg....gwkg...",
            "gggggggggggggggg",
            "gGgGgGgGgGgGgGgg",
            "gggggggggggggggg",
            "g..rrrr..rrrr..g",
            "g.rrrrrrrrrrrr.g",
            "g..wkww..wwkw..g",
            "g..wwww..wwww..g",
            "gggggg.kk.gggggg",
            "gggggg.kk.gggggg",
            "................",
        ],
        ["village"] =
        [
            "................",
            "................",
            "...yyy..........",
            "..yyyyy....yyy..",
            ".yyyyyyy..yyyyy.",
            "..nwnnn..yyyyyyy",
            "..nknnn...nnwnn.",
            "..nnnnn...nnknn.",
            "..........nnnnn.",
            "....yyy.........",
            "...yyyyy........",
            "..yyyyyyy.......",
            "...nnwnn........",
            "...nnknn........",
            "...nnnnn........",
            "................",
        ],
        ["castle"] =
        [
            "g.g.g.......g.g.",
            "ggggg..r....gggg",
            "gkgkg..rr...gkgk",
            "ggggg..r....gggg",
            "ggggg.g.g.g.gggg",
            "gggggggggggggggg",
            "gGgkgGgggGgkgGgg",
            "gggggggggggggggg",
            "ggkggggggggggkgg",
            "gggggggggggggggg",
            "gggggggkkggggggg",
            "ggggggkkkkgggggg",
            "ggggggkkkkgggggg",
            "ggggggkkkkgggggg",
            "GGGGGGGGGGGGGGGG",
            "................",
        ],
        ["shrine"] =
        [
            "................",
            ".......oo.......",
            "......oyyo......",
            ".......oo.......",
            "......wwww......",
            "....wwwwwwww....",
            "...wwwwwwwwww...",
            "....wgwwwwgw....",
            "....wgwkkwgw....",
            "....wgwkkwgw....",
            "....wgwkkwgw....",
            "...wwwwwwwwww...",
            "..gggggggggggg..",
            "................",
            "................",
            "................",
        ],
        ["dungeon"] =
        [
            "................",
            "................",
            ".....mmmmmm.....",
            "...mmMMmmmmmm...",
            "..mmMMmmmmmmmm..",
            ".mmMmmkkkkmmmmm.",
            ".mmmmkkkkkkmmmm.",
            "mmMmkkkkkkkkmmmm",
            "mmmmkkkkkkkkmmmm",
            "mmmkkkkkkkkkkmmm",
            "mmmkkkkkkkkkkmmm",
            "mmmkkkkkkkkkkmmm",
            ".mmkkkkkkkkkkmm.",
            "................",
            "................",
            "................",
        ],
        ["ship"] =
        [
            "........r.......",
            ".......nrr......",
            "......wn........",
            ".....wwnw.......",
            "....wwwnww......",
            "...wwwwnwww.....",
            "..wwwwwnwwww....",
            "...wwwwnwww.....",
            ".......n........",
            "..nnnnnnnnnnnn..",
            "..nNnNnNnNnNnn..",
            "...nnnnnnnnnn...",
            "....nnnnnnnn....",
            "................",
            "................",
            "................",
        ],
        ["flowers"] =
        [
            "................",
            "..r.......y.....",
            ".rkr.....yky....",
            "..r...o...y.....",
            "..h..oko..h.....",
            "..h...o...h..r..",
            "......h.....rkr.",
            "..y...h......r..",
            ".yky.........h..",
            "..y.....r....h..",
            "..h....rkr......",
            "..h.....r...o...",
            "........h..oko..",
            "........h...o...",
            "............h...",
            "................",
        ],
        ["door"] =
        [
            "NNNNNNNNNNNNNNNN",
            "NnnnNnnnNnnnNnnN",
            "NnnnNnnnNnnnNnnN",
            "NnnnNnnnNnnnNnnN",
            "NnnnNnnnNnnnNnnN",
            "NnnnNnnnNnnnNnnN",
            "NnnnNnnnNnnnNnnN",
            "NnnnNnnnNnnnNyyN",
            "NnnnNnnnNnnnNyyN",
            "NnnnNnnnNnnnNnnN",
            "NnnnNnnnNnnnNnnN",
            "NnnnNnnnNnnnNnnN",
            "NnnnNnnnNnnnNnnN",
            "NnnnNnnnNnnnNnnN",
            "NnnnNnnnNnnnNnnN",
            "NNNNNNNNNNNNNNNN",
        ],
        ["counter"] =
        [
            "................",
            "................",
            "................",
            "NNNNNNNNNNNNNNNN",
            "nnnnnnnnnnnnnnnn",
            "nnnnnnnnnnnnnnnn",
            "nnnnnnnnnnnnnnnn",
            "NNNNNNNNNNNNNNNN",
            "xnnnxnnnxnnnxnnn",
            "xnnnxnnnxnnnxnnn",
            "xnnnxnnnxnnnxnnn",
            "xnnnxnnnxnnnxnnn",
            "xnnnxnnnxnnnxnnn",
            "NNNNNNNNNNNNNNNN",
            "................",
            "................",
        ],
        ["bed"] =
        [
            "................",
            ".NNNNNNNNNNNNNN.",
            ".NwwwwwNnnnnnnN.",
            ".NwwwwwNnnnnnnN.",
            ".NNNNNNNNNNNNNN.",
            ".NaaaaaaaaaaaaN.",
            ".NaaaaaaaaaaaaN.",
            ".NaacaaaacaaaaN.",
            ".NaaaaaaaaaaaaN.",
            ".NaaaaaaaaaaaaN.",
            ".NaaaacaaaaacaN.",
            ".NaaaaaaaaaaaaN.",
            ".NbbbbbbbbbbbbN.",
            ".NNNNNNNNNNNNNN.",
            ".N............N.",
            "................",
        ],
        ["table"] =
        [
            "................",
            "................",
            "................",
            "..NNNNNNNNNNNN..",
            ".NnnnnnnnnnnnnN.",
            ".NnnwnnnnnnynnN.",
            ".NnnnnnnnnnnnnN.",
            ".NnnnnnnnnnnnnN.",
            "..NNNNNNNNNNNN..",
            "..N..........N..",
            "..N..........N..",
            "..N..........N..",
            "..N..........N..",
            "................",
            "................",
            "................",
        ],
        ["shelf"] =
        [
            "NNNNNNNNNNNNNNNN",
            "NrruuyrgguurryyN",
            "NrruuyrgguurryyN",
            "NrruuyrgguurryyN",
            "NNNNNNNNNNNNNNNN",
            "NuyyrrgguurryrrN",
            "NuyyrrgguurryrrN",
            "NuyyrrgguurryrrN",
            "NNNNNNNNNNNNNNNN",
            "NggrruuyyrrguuyN",
            "NggrruuyyrrguuyN",
            "NggrruuyyrrguuyN",
            "NNNNNNNNNNNNNNNN",
            "N..............N",
            "N..............N",
            "NNNNNNNNNNNNNNNN",
        ],
        ["grave"] =
        [
            "................",
            "................",
            ".....gggggg.....",
            "....gMMMMMMg....",
            "....gMMMMMMg....",
            "....gMMkMMMg....",
            "....gMkkkMMg....",
            "....gMMkMMMg....",
            "....gMMMMMMg....",
            "....gMMMMMMg....",
            "....gMMMMMMg....",
            "...gggggggggg...",
            "...NNNNNNNNNN...",
            "................",
            "................",
            "................",
        ],
        ["statue"] =
        [
            "................",
            ".......MM.......",
            "......MMMM......",
            ".......MM.......",
            ".....MMMMMM.....",
            "....MM.MM.MM....",
            "....M..MM..M....",
            ".......MM.......",
            "......MMMM......",
            "......M..M......",
            "......M..M......",
            ".....MM..MM.....",
            "...gggggggggg...",
            "...gMMMMMMMMg...",
            "...gggggggggg...",
            "................",
        ],
        ["well"] =
        [
            "................",
            "..nnnnnnnnnnnn..",
            "..n..........n..",
            "..n.NNNNNNNN.n..",
            "..n.N......N.n..",
            "..n.....g....n..",
            "..n.....g....n..",
            "..gggggggggggg..",
            ".gMgMgMgMgMgMgg.",
            ".gzzzzzzzzzzzzg.",
            ".gzZzzzzzzzZzzg.",
            ".gzzzzzzzzzzzzg.",
            ".gMgMgMgMgMgMgg.",
            "..gggggggggggg..",
            "................",
            "................",
        ],
        ["chest"] =
        [
            "................",
            "................",
            "................",
            "................",
            "...NNNNNNNNNN...",
            "..NnnnnnnnnnnN..",
            "..NnnnnnnnnnnN..",
            "..NyyyyyyyyyyN..",
            "..NnnnnyynnnnN..",
            "..NnnnnkknnnnN..",
            "..NnnnnnnnnnnN..",
            "..NnnnnnnnnnnN..",
            "..NyyyyyyyyyyN..",
            "...NNNNNNNNNN...",
            "................",
            "................",
        ],
        ["throne"] =
        [
            "....yyyyyyyy....",
            "....yrrrrrry....",
            "....yrrrrrry....",
            "....yrrrrrry....",
            "....yrrrrrry....",
            "....yrrrrrry....",
            "...yyyyyyyyyy...",
            "...yrrrrrrrry...",
            "...yrrrrrrrry...",
            "...yyyyyyyyyy...",
            "...y........y...",
            "...y........y...",
            "...y........y...",
            "..yyy......yyy..",
            "................",
            "................",
        ],
        ["fence"] =
        [
            "................",
            "................",
            "..n...n...n...n.",
            ".nnn.nnn.nnn.nnn",
            "nnnnnnnnnnnnnnnn",
            "..n...n...n...n.",
            "..n...n...n...n.",
            "nnnnnnnnnnnnnnnn",
            "..n...n...n...n.",
            "..n...n...n...n.",
            "..n...n...n...n.",
            "..N...N...N...N.",
            "................",
            "................",
            "................",
            "................",
        ],
        ["fountain"] =
        [
            "................",
            ".......ZZ.......",
            "......Z.wZ......",
            ".......ZZ.......",
            ".......gg.......",
            "....ZZ.gg.ZZ....",
            "...Z...gg...Z...",
            "..gggggggggggg..",
            ".gMMMMMMMMMMMMg.",
            ".gZZzzZZzzZZzzg.",
            ".gzzZZzzZZzzZZg.",
            ".gZZzzZZzzZZzzg.",
            ".gMMMMMMMMMMMMg.",
            "..gggggggggggg..",
            "................",
            "................",
        ],
        ["tree"] =
        [
            "................",
            "......HHHH......",
            "....HHhhhhHH....",
            "...HhhjhhhhhH...",
            "..HhhjjhhhhhhH..",
            "..HhhhhhhhjhhH..",
            "..HhhhhhhjjhhH..",
            "..HhhhhhhhhhhH..",
            "...HhhhhhhhhH...",
            "....HHhhhhHH....",
            "......HnnH......",
            ".......nn.......",
            ".......nn.......",
            "......nnnn......",
            "................",
            "................",
        ],
        ["oak"] =
        [
            "....HHHHHHHH....",
            "..HHhhhhhhhhHH..",
            ".HhhjhhhhhjhhhH.",
            "HhhjjhhhhhjjhhhH",
            "HhhhhhhjhhhhhhhH",
            "HhhhhhjjhhhhhhhH",
            ".HhhhhhhhhhhjhH.",
            "..HHhhhhhhhhhH..",
            "....HHHnnHHH....",
            "......NnnN......",
            "......NnnN......",
            "......NnnN......",
            ".....NnnnnN.....",
            "....NN.nn.NN....",
            "................",
            "................",
        ],
    };

    /// <summary>The colours of each place in the party.</summary>
    public static readonly uint[] PartyTints = [0xFF20C8D0, 0xFFC04040, 0xFFE0C040, 0xFF40A060, 0xFFA060C0, 0xFFE08030, 0xFF4080D0, 0xFFD0D0D0];

    public static uint Parse(string hex) => 0xFF000000 | Convert.ToUInt32(hex.TrimStart('#'), 16);

    public static uint Shade(uint argb, double f)
    {
        uint r = (argb >> 16) & 0xFF, g = (argb >> 8) & 0xFF, b = argb & 0xFF;
        uint S(uint v) => (uint)Math.Clamp(f >= 1 ? v + (255 - v) * (f - 1) : v * f, 0, 255);
        return (argb & 0xFF000000) | (S(r) << 16) | (S(g) << 8) | S(b);
    }

    public static uint Mix(uint a, uint b, double t)
    {
        uint L(int shift) => (uint)Math.Clamp(((a >> shift) & 0xFF) * (1 - t) + ((b >> shift) & 0xFF) * t, 0, 255) << shift;
        return 0xFF000000 | L(16) | L(8) | L(0);
    }

    /// <summary>The colour of a pixel letter, given the sprite's tint; 0 for clear.</summary>
    public static uint Colour(char ch, uint tint) => ch switch
    {
        '.' => 0,
        'a' => tint,
        'b' => Shade(tint, 0.62),
        'c' => Shade(tint, 1.45),
        _ => Fixed.GetValueOrDefault(ch, 0xFFFF00FF),
    };

    /// <summary>A drawing's pixels, as ARGB, sixteen to a row.</summary>
    public static uint[] Pixels(string name, uint tint, bool flip = false)
    {
        var rows = All[name];
        var px = new uint[256];
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
            px[y * 16 + x] = Colour(rows[y][flip ? 15 - x : x], tint);
        return px;
    }

    // ───────────────────────────── the ground of the open land ─────────────────────────────

    private static uint Hash(int x, int y, int salt)
    {
        uint h = (uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791);
        h ^= h >> 13;
        h *= 0x5bd1e995;
        return h ^ (h >> 15);
    }

    private static void Disc(uint[] px, double cx, double cy, double r, uint col, uint light)
    {
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
        {
            double dx = x + 0.5 - cx, dy = y + 0.5 - cy;
            if (dx * dx + dy * dy > r * r) continue;
            px[y * 16 + x] = dx + dy < -r * 0.5 ? light : col;
        }
    }

    /// <summary>
    /// Paints a square of the open land: grass speckled with tufts, scrub with bushes, forest crowns,
    /// rounded hills, snow-capped peaks, marsh pools, sand, and water whose waves move with the frame.
    /// For the land, the frame picks one of a few variants, so that a range of hills or a forest
    /// doesn't look stamped out of one block.
    /// </summary>
    public static uint[] Ground(string kind, int frame)
    {
        var px = new uint[256];
        int v = frame % 3;
        uint grass = 0xFF4E9A3A, grassDark = 0xFF3E8030, grassLight = 0xFF66B048;
        void Base(uint a, uint b, uint c, int salt)
        {
            for (int i = 0; i < 256; i++)
            {
                uint h = Hash(i % 16, i / 16, salt) % 100;
                px[i] = h < 10 ? b : h > 92 ? c : a;
            }
        }
        switch (kind)
        {
            case "grass":
                Base(grass, grassDark, grassLight, 1 + v * 31);
                break;
            case "brush":
                Base(grass, grassDark, grassLight, 2 + v * 31);
                foreach (var (bx, by, br) in v switch
                {
                    0 => new[] { (4.0, 5.0, 2.4), (11.0, 4.0, 2.0), (8.0, 11.0, 2.6), (13.0, 12.0, 1.6) },
                    1 => new[] { (3.0, 10.0, 2.2), (9.0, 6.0, 2.6), (13.0, 13.0, 1.8) },
                    _ => new[] { (5.0, 3.0, 1.8), (12.0, 8.0, 2.4), (5.0, 12.0, 2.2) },
                })
                    Disc(px, bx, by, br, 0xFF2E6A2A, 0xFF4A8A3A);
                break;
            case "forest":
                Base(0xFF2A5224, 0xFF1E3E1A, 0xFF34602C, 3 + v * 31);
                foreach (var (cx, cy, r) in v switch
                {
                    0 => new[] { (4.5, 4.5, 4.2), (12.0, 4.0, 3.8), (8.5, 10.5, 4.6), (2.5, 13.0, 3.0), (14.5, 12.5, 3.0) },
                    1 => new[] { (3.5, 3.5, 3.6), (10.5, 5.5, 4.4), (4.5, 11.0, 4.0), (13.0, 13.0, 3.2) },
                    _ => new[] { (8.0, 3.5, 4.0), (3.0, 8.5, 3.6), (12.5, 9.5, 4.0), (7.0, 13.5, 3.4) },
                })
                {
                    Disc(px, cx, cy, r, 0xFF2E7A2C, 0xFF4CA03E);
                    if (cy + r < 15) px[(int)(cy + r) * 16 + (int)cx] = 0xFF5A3A20;
                }
                break;
            case "hills":
                Base(grass, grassDark, grassLight, 4 + v * 31);
                foreach (var (cx, cy, r) in v switch
                {
                    0 => new[] { (5.0, 9.0, 5.0), (11.5, 11.0, 4.5) },
                    1 => new[] { (8.0, 8.0, 6.0), (3.0, 14.0, 3.0) },
                    _ => new[] { (4.0, 13.0, 4.0), (11.0, 7.0, 5.0) },
                })
                    for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        double dx = x + 0.5 - cx, dy = y + 0.5 - cy;
                        if (dy > 1.5 || dx * dx / (r * r) + dy * dy / (r * r * 0.45) > 1) continue;
                        px[y * 16 + x] = dx < -r * 0.3 ? 0xFF8AB060 : dx > r * 0.35 ? 0xFF4A7A34 : 0xFF6A9A48;
                    }
                break;
            case "mountains":
                Base(0xFF5A5650, 0xFF4A4642, 0xFF6A6660, 5 + v * 31);
                foreach (var (peak, w, h0) in v switch
                {
                    0 => new[] { (5.0, 6.0, 1.0), (11.0, 6.5, 3.0) },
                    1 => new[] { (8.0, 8.5, 0.0) },
                    _ => new[] { (11.0, 6.0, 1.0), (4.0, 5.5, 4.0) },
                })
                    for (int y = (int)h0; y < 16; y++)
                    {
                        double half = (y - h0) / (15 - h0) * w;
                        for (int x = 0; x < 16; x++)
                        {
                            double dx = x + 0.5 - peak;
                            if (Math.Abs(dx) > half) continue;
                            bool snow = y < h0 + 4 && Math.Abs(dx) < half * 0.9;
                            px[y * 16 + x] = snow ? (dx < 0 ? 0xFFF4F4F8 : 0xFFC8C8D4) : dx < 0 ? 0xFF9A948A : 0xFF6E6860;
                        }
                    }
                break;
            case "swamp":
                Base(0xFF4A5A30, 0xFF3A4826, 0xFF5A6A3A, 6);
                Disc(px, 5, 6, 2.6, 0xFF2A3A30, 0xFF3A5040);
                Disc(px, 11, 11, 3.0, 0xFF2A3A30, 0xFF3A5040);
                foreach (var (x, y) in new[] { (12, 3), (13, 4), (3, 12), (2, 13), (8, 2) })
                {
                    px[y * 16 + x] = 0xFF8A9A40;
                    if (y > 0) px[(y - 1) * 16 + x] = 0xFF8A9A40;
                }
                break;
            case "sand":
                Base(0xFFD8C080, 0xFFC0A868, 0xFFE8D498, 7);
                break;
            case "deep" or "water" or "shallows":
            {
                (uint a, uint b, uint wave) = kind switch
                {
                    "deep" => (0xFF1A3A70u, 0xFF16325Eu, 0xFF3A5A96u),
                    "water" => (0xFF2A64A8u, 0xFF245898u, 0xFF6AA0DCu),
                    _ => (0xFF4A90C8u, 0xFF4084B8u, 0xFFA0D0F0u),
                };
                Base(a, b, a, 8);
                // Short wave crests that drift sideways with the frame.
                foreach (var (x, y) in new[] { (2, 3), (9, 2), (5, 8), (12, 7), (1, 12), (9, 13) })
                    for (int i = 0; i < 3; i++)
                        px[y * 16 + (x + i + frame * 2) % 16] = wave;
                if (kind == "shallows")
                    foreach (var (x, y) in new[] { (13, 11), (4, 5) })
                        px[y * 16 + x] = 0xFFC8B880;
                break;
            }
            // In town: cobbles, floorboards, brick.
            case "path":
                Base(0xFF8A8478, 0xFF7A7468, 0xFF9A9488, 9);
                for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    if ((y % 4 == 3) || ((x + (y / 4) * 2) % 5 == 4)) px[y * 16 + x] = 0xFF6A6458;
                break;
            case "floor":
                for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    bool seam = y % 4 == 3 || (x + (y / 4) * 6) % 16 == 0;
                    px[y * 16 + x] = seam ? 0xFF6A4A28 : Hash(x, y, 10) % 9 == 0 ? 0xFF8E6436 : 0xFF9A6E3E;
                }
                break;
            case "wall":
                for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    bool mortar = y % 4 == 3 || (x + (y / 4 % 2) * 4) % 8 == 7;
                    px[y * 16 + x] = mortar ? 0xFF4A4650 : y % 4 == 0 ? 0xFFA8A4B0 : 0xFF8C8894;
                }
                break;
            default:
                Base(grass, grassDark, grassLight, 1);
                break;
        }
        return px;
    }

    // ───────────────────────────── bitmaps ─────────────────────────────

    private static readonly Dictionary<(string, uint, bool, int), Bitmap> Cache = [];

    public static Bitmap Bitmap(uint[] px)
    {
        var wb = new WriteableBitmap(new PixelSize(16, 16), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using (var fb = wb.Lock())
        {
            unsafe
            {
                for (int y = 0; y < 16; y++)
                    px.AsSpan(y * 16, 16).CopyTo(new Span<uint>((void*)(fb.Address + y * fb.RowBytes), 16));
            }
        }
        return wb;
    }

    /// <summary>A sprite in a tint, as a bitmap (made once, then kept).</summary>
    public static Bitmap Get(string name, uint tint, bool flip = false)
    {
        var key = (name, tint, flip, -1);
        if (!Cache.TryGetValue(key, out var b)) Cache[key] = b = Bitmap(Pixels(name, tint, flip));
        return b;
    }

    /// <summary>A square of ground, as a bitmap.</summary>
    public static Bitmap GroundBitmap(string kind, int frame = 0)
    {
        var key = (kind, 0u, false, frame);
        if (!Cache.TryGetValue(key, out var b)) Cache[key] = b = Bitmap(Ground(kind, frame));
        return b;
    }
}
