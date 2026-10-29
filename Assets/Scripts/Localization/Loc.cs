using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// English / Thai switch.
///  - Loc.T("English text") returns the Thai version when Thai is on (unknown text is returned unchanged).
///  - Add your own lines in the Thai dictionary below (English -> Thai).
///  - A button "EN / ไทย" is created automatically at the top-right while playing (no scene setup).
///  - A Thai font is made at runtime from the computer's own font (nothing to import).
/// </summary>
public static class Loc
{
    public static bool Thai { get; private set; }
    public static event System.Action Changed;

    static Loc() { Thai = PlayerPrefs.GetInt("lang_thai", 0) == 1; }

    public static void Set(bool thai)
    {
        Thai = thai;
        PlayerPrefs.SetInt("lang_thai", thai ? 1 : 0);
        Changed?.Invoke();
    }

    public static string T(string english)
    {
        if (!Thai || string.IsNullOrEmpty(english)) return english;
        if (Dict.TryGetValue(english, out string th)) return Shape(th);
        return Shape(Rule(english));
    }

    // TextMeshPro cannot swap a tone mark for its "narrow" shape after tall letters (ป ฝ ฟ ฬ),
    // so the mark would hide inside the letter. The font (Sarabun in Resources/Fonts) has the
    // narrow marks at U+F713..F716; this puts them in. Safe to call twice.
    // How high (in em) a tone mark is lifted above an upper vowel. Change if it looks too low / high.
    public const string MarkLift = "0.22";
    // Extra gap (em) between a tone mark and the following sara am (ำ)
    public const string AmGap = "0.12";

    public static string Shape(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            sb.Append(c);
            // Tone mark on top of an upper vowel (ที่ ปี่ ...): lift the mark so it does not overlap the vowel
            bool upperVowel = c == '\u0E31' || (c >= '\u0E34' && c <= '\u0E37') || c == '\u0E47';
            if (upperVowel && i + 1 < s.Length && s[i + 1] >= '\u0E48' && s[i + 1] <= '\u0E4B')
            {
                sb.Append("<voffset=" + MarkLift + "em>").Append(s[i + 1]).Append("</voffset>");
                i++;
                continue;
            }
            if ((c == '\u0E1B' || c == '\u0E1D' || c == '\u0E1F' || c == '\u0E2C') && i + 1 < s.Length)
            {
                char n = s[i + 1];
                // a lower vowel may sit between the tall letter and the mark (ฝุ่น ปุ่ม)
                if ((n == '\u0E38' || n == '\u0E39') && i + 2 < s.Length)
                {
                    char m2 = s[i + 2];
                    if (m2 >= '\u0E48' && m2 <= '\u0E4B') { sb.Append(n).Append((char)(0xF713 + (m2 - 0x0E48))); i += 2; continue; }
                }
                if (n >= '\u0E48' && n <= '\u0E4B') { sb.Append((char)(0xF713 + (n - 0x0E48))); i++; }
            }
        }
        // Tone mark right before sara am (ซ้ำ น้ำ): push the "ำ" a little to the right so they do not touch
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "([\u0E48-\u0E4B\uF713-\uF716])(?=\u0E33)",
            "<cspace=" + AmGap + "em>$1</cspace>");
    }

    private static string OrdNum(string w)
    {
        return w == "first" ? "1" : w == "second" ? "2" : w == "third" ? "3" : "4";
    }

    // Texts that contain numbers or pieces (counters, hints...)
    private static string Rule(string s)
    {
        System.Text.RegularExpressions.Match m;
        m = System.Text.RegularExpressions.Regex.Match(s, @"^Bag \[I\] \((\d+)\)$");
        if (m.Success) return "กระเป๋า [I] (" + m.Groups[1].Value + ")";
        m = System.Text.RegularExpressions.Regex.Match(s, @"^Case Book \[Tab\] \((\d+)\)$");
        if (m.Success) return "สมุดเคส [Tab] (" + m.Groups[1].Value + ")";
        m = System.Text.RegularExpressions.Regex.Match(s, @"^Hint \[H\] \((\d+) left\)$");
        if (m.Success) return "คำใบ้ [H] (เหลือ " + m.Groups[1].Value + ")";
        m = System.Text.RegularExpressions.Regex.Match(s, @"^(.*?) ?The (first|second|third|fourth) digit of the code is (\d)\.$", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (m.Success)
        {
            string pre = m.Groups[1].Value.Length > 0 ? T(m.Groups[1].Value) + " " : "";
            return pre + "เลขหลักที่ " + OrdNum(m.Groups[2].Value) + " ของรหัสคือ " + m.Groups[3].Value;
        }
        m = System.Text.RegularExpressions.Regex.Match(s, @"^Code: (first|second|third|fourth) digit$");
        if (m.Success) return "รหัส: หลักที่ " + OrdNum(m.Groups[1].Value);
        m = System.Text.RegularExpressions.Regex.Match(s, @"^Hint (\d+)$");
        if (m.Success) return "คำใบ้ " + m.Groups[1].Value;
        m = System.Text.RegularExpressions.Regex.Match(s, @"^Enter the (\d+)-digit code$");
        if (m.Success) return "ใส่รหัส " + m.Groups[1].Value + " หลัก";
        m = System.Text.RegularExpressions.Regex.Match(s, @"^HINT: Look at (.+)\.( That was your LAST hint\. Ask again and you die\.)?$");
        if (m.Success)
            return "คำใบ้: ดูที่" + T(m.Groups[1].Value) + (m.Groups[2].Success ? T(m.Groups[2].Value) : "");
        m = System.Text.RegularExpressions.Regex.Match(s, @"^HINT: (.+)$");
        if (m.Success) return "คำใบ้: " + T(m.Groups[1].Value);
        m = System.Text.RegularExpressions.Regex.Match(s, @"^YOU DIED\n<size=50%>Return by death\.\.\. Loop (\d+)</size>$");
        if (m.Success) return "คุณตายแล้ว\n<size=50%>กลับมาด้วยความตาย... รอบที่ " + m.Groups[1].Value + "</size>";
        m = System.Text.RegularExpressions.Regex.Match(s, @"^<b>(.+)</b>\n\n(.+)$", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (m.Success) return "<b>" + T(m.Groups[1].Value) + "</b>\n\n" + T(m.Groups[2].Value);
        m = System.Text.RegularExpressions.Regex.Match(s, @"^(.+?): (.+)$", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (m.Success && Dict.ContainsKey(m.Groups[1].Value)) return T(m.Groups[1].Value) + ": " + T(m.Groups[2].Value);
        return s;
    }

    // ---- ADD / EDIT TRANSLATIONS HERE (English text must match exactly) ----
    private static readonly Dictionary<string, string> Dict = new Dictionary<string, string>
    {
        { "INCOMING CALL...", "สายเรียกเข้า..." },
        { "UNKNOWN CALLER", "ไม่ทราบชื่อผู้โทร" },
        { "WRONG.", "ผิด!" },
        { "WRONG. 2 left", "ผิด! เหลืออีก 2 ครั้ง" },
        { "WRONG. 1 left", "ผิด! เหลืออีก 1 ครั้ง" },
        { "CLICK. Unlocked.", "แกร๊ก ปลดล็อกแล้ว" },
        { "The lock is already open.", "ล็อกนี้เปิดอยู่แล้ว" },
        { "The lock clicks open.", "ล็อกเปิดออกแล้ว" },
        { "He backs away into the dark. Keep going...", "เขาถอยหายเข้าไปในความมืด ทำต่อไป..." },
        { "Wrong. I gave you four digits to find and you still managed to fail.", "ผิด ฉันให้เลขสี่ตัวให้หา แล้วเธอก็ยังพลาดได้" },
        { "Did you even LOOK at the notes? Pathetic.", "เธอดูโน้ตบ้างหรือเปล่า น่าสมเพช" },
        { "A child could have opened that lock. Goodnight.", "เด็กยังเปิดล็อกนั้นได้เลย ราตรีสวัสดิ์" },
        { "That was the wrong code. You know what happens now, right?", "รหัสผิด รู้ใช่ไหมว่าต่อไปจะเกิดอะไร" },
        { "Hey! Where do you think you're going? Not that way, idiot.", "เฮ้! จะไปไหนน่ะ ไม่ใช่ทางนั้นโว้ย ไอ้โง่" },
        { "Stop wandering. The way out is NOT down there.", "เลิกเดินมั่ว ทางออกไม่ได้อยู่ทางนั้น" },
        { "Do you want to die? Because that is where you die.", "อยากตายหรือไง ตรงนั้นแหละที่เธอจะตาย" },
        { "I told you. I TOLD you. Not that way.", "ฉันบอกแล้ว ฉันบอกแล้วไงว่าไม่ใช่ทางนั้น" },
        { "That is what you get for ignoring me.", "นี่แหละผลของการไม่ฟังฉัน" },
        { "...and now you are dead. Again. Impressive.", "...แล้วเธอก็ตายอีกแล้ว เก่งจริงๆ" },
        { "Loop {loop}. Welcome back. Try to be less useless this time.", "รอบที่ {loop} ยินดีต้อนรับกลับมา รอบนี้พยายามอย่าไร้ประโยชน์นักล่ะ" },
        { "The code has changed. Everything you remembered about it is garbage.", "รหัสเปลี่ยนแล้ว ที่เธอจำได้ใช้ไม่ได้เลยสักอย่าง" },
        { "Do not die again. It is getting boring.", "อย่าตายอีก เริ่มน่าเบื่อแล้ว" },
        { "Really? You need help ALREADY?", "จริงเหรอ ขอความช่วยเหลือแล้วเนี่ยนะ" },
        { "Fine. I will hold your hand. Again.", "ก็ได้ ฉันจะจูงมือเธอ อีกแล้ว" },
        { "How are you this slow? Listen carefully.", "ช้าอะไรขนาดนี้ ฟังให้ดี" },
        { "Ugh. Do I have to do everything myself?", "เฮ้อ ต้องให้ฉันทำเองทุกอย่างเลยหรือไง" },
        { "I gave you two hints. TWO. And you want a third?", "ฉันให้คำใบ้ไปสองครั้ง สองนะ แล้วจะเอาครั้งที่สามอีกเหรอ" },
        { "No more help. You used it all. Goodbye.", "หมดแล้ว เธอใช้จนหมด ลาก่อน" },
        { "Begging for hints again? That is the end of the line.", "ขอคำใบ้อีกแล้วเหรอ จบกันแค่นี้" },
        { "YOU DIED", "คุณตายแล้ว" },
        { "LOOP", "การวนเวียน" },
        { "(Somewhere in the room, a phone starts to ring...)", "(ที่ไหนสักแห่งในห้อง โทรศัพท์เริ่มดังขึ้น...)" },
        { "...Something about the portrait on the wall looks different now.", "...รูปภาพบนผนังดูแปลกไปจากเมื่อกี้" },
        { "A cabinet labelled 'M'. The folder for 'Mali' has been cut out with scissors.", "ตู้เก็บเอกสารติดป้าย 'M' แฟ้มของ 'มะลิ' ถูกตัดออกไปด้วยกรรไกร" },
        { "A faded family portrait. Every face has been scratched out.", "ภาพครอบครัวสีซีด ใบหน้าทุกคนถูกขูดออกหมด" },
        { "A hospital bed. The sheets are crumpled, as if someone just got up.", "เตียงผู้ป่วย ผ้าปูยับยู่ยี่ราวกับมีคนเพิ่งลุกไป" },
        { "A notice pinned to the wall: 'Clinic closed until further notice. Do not remove any records.'", "ประกาศติดผนัง: 'คลินิกปิดจนกว่าจะมีประกาศใหม่ ห้ามนำเอกสารออกไป'" },
        { "A reception desk covered in dust. Something was dragged across it a long time ago.", "โต๊ะต้อนรับเต็มไปด้วยฝุ่น มีบางอย่างถูกลากผ่านไปนานมาแล้ว" },
        { "A wooden bench. Someone carved 'HELP' into the seat.", "ม้านั่งไม้ มีคนแกะคำว่า 'HELP' ไว้บนที่นั่ง" },
        { "Picked up: Old Key", "เก็บแล้ว: กุญแจเก่า" },
        { "Picked up: Patient Register", "เก็บแล้ว: ทะเบียนผู้ป่วย" },
        { "Picked up: Torn Photo", "เก็บแล้ว: รูปถ่ายที่ฉีกขาด" },
        { "Rows of filing cabinets. Every drawer is open and empty.", "ตู้เก็บเอกสารเรียงเป็นแถว ลิ้นชักเปิดทุกอันและว่างเปล่า" },
        { "Something shatters on the floor nearby!", "มีบางอย่างแตกกระจายบนพื้นใกล้ๆ!" },
        { "The door is locked. I need a key.", "ประตูล็อกอยู่ ต้องใช้กุญแจ" },
        { "The door will not open. I feel like I'm missing something I should have found...", "ประตูไม่เปิด เหมือนฉันยังหาอะไรบางอย่างไม่เจอ..." },
        { "The exit is sealed. There is a keypad next to it, and I should look around first.", "ทางออกถูกปิดตาย มีแป้นกดรหัสอยู่ข้างๆ ควรสำรวจรอบๆ ก่อน" },
        { "The faces are not scratched out anymore. Now they all stare at you.", "ใบหน้าไม่ได้ถูกขูดออกแล้ว ตอนนี้ทุกคนจ้องมาที่คุณ" },
        { "The key turns. The door creaks open...", "กุญแจหมุน ประตูเปิดออกพร้อมเสียงเอี๊ยด..." },
        { "The lights flicker. Then everything goes silent.", "ไฟกะพริบ แล้วทุกอย่างก็เงียบสนิท" },
        { "The medicine bottles are empty. Every label has been scratched off.", "ขวดยาว่างเปล่า ฉลากทุกอันถูกขูดออก" },
        { "The mirror shows the room behind you. But you are not in it.", "กระจกสะท้อนห้องด้านหลังคุณ แต่ไม่มีคุณอยู่ในนั้น" },
        { "The name circled in the register is 'Mali'. The girl in the torn photo must be her.", "ชื่อที่วงไว้ในทะเบียนคือ 'มะลิ' เด็กผู้หญิงในรูปที่ฉีกขาดคงเป็นเธอ" },
        { "The phone is dead. The cord was cut years ago.", "โทรศัพท์ใช้การไม่ได้ สายถูกตัดมาหลายปีแล้ว" },
        { "The two clues fit together. The lock gives way...", "เบาะแสทั้งสองเข้ากันพอดี ล็อกคลายออก..." },
        { "! Careful: a drug addict may attack while you enter the code. Keep checking left and right!", "! ระวัง: คนติดยาอาจโจมตีระหว่างที่คุณใส่รหัส คอยมองซ้ายขวาไว้!" },
        { "<  BACK", "<  กลับ" },
        { "BACK", "กลับ" },
        { "BACK  >", "กลับ  >" },
        { "<  LOOK LEFT", "<  มองซ้าย" },
        { "LOOK RIGHT  >", "มองขวา  >" },
        { "<  Prev", "<  ก่อนหน้า" },
        { "Next  >", "ถัดไป  >" },
        { "CLR", "ล้าง" },
        { "KEYPAD", "แป้นรหัส" },
        { "CASE BOOK", "สมุดเคส" },
        { "Close [Esc]", "ปิด [Esc]" },
        { "A/D = walk  |  W = jump  |  S = crouch  |  Tab = Case Book  |  Click objects to inspect  |  Explore the clinic", "A/D = เดิน  |  W = กระโดด  |  S = ย่อ  |  Tab = สมุดเคส  |  คลิกวัตถุเพื่อสำรวจ  |  สำรวจคลินิก" },
        { "BASEMENT", "ห้องใต้ดิน" },
        { "TO THE RECORDS ROOM", "ไปห้องเก็บบันทึก" },
        { "TO THE WARD", "ไปหอผู้ป่วย" },
        { "ROOM 1: WAITING HALL", "ห้อง 1: ห้องรอ" },
        { "ROOM 2: WARD", "ห้อง 2: หอผู้ป่วย" },
        { "ROOM 3: RECORDS ROOM", "ห้อง 3: ห้องเก็บบันทึก" },
        { "Room 1: Waiting Hall", "ห้อง 1: ห้องรอ" },
        { "Room 2: Ward", "ห้อง 2: หอผู้ป่วย" },
        { "Room 3: Records Room", "ห้อง 3: ห้องเก็บบันทึก" },
        { "PHONE CALL\n? - ? - ? - ?", "สายโทรศัพท์\n? - ? - ? - ?" },
        { "You look to the left. Only darkness...", "คุณมองไปทางซ้าย มีแต่ความมืด..." },
        { "You look to the right. Only darkness...", "คุณมองไปทางขวา มีแต่ความมืด..." },
        { "(No clues yet. Look around and click things.)", "(ยังไม่มีเบาะแส ลองสำรวจรอบๆ แล้วคลิกวัตถุ)" },
        { "You already have every piece. Open the Case Book, put the digits in order and use the keypad by the exit.", "คุณได้ครบทุกชิ้นแล้ว เปิดสมุดเคส เรียงตัวเลขให้ถูก แล้วใช้แป้นรหัสข้างทางออก" },
        { "Search again. Something has not been checked yet.", "ลองหาอีกรอบ ยังมีบางอย่างที่ยังไม่ได้ตรวจ" },
        { "Closure notice", "ประกาศปิดคลินิก" },
        { "Connection", "ความเชื่อมโยง" },
        { "Dead phone", "โทรศัพท์ที่เสีย" },
        { "Family portrait", "ภาพครอบครัว" },
        { "Medicine cabinet", "ตู้ยา" },
        { "Missing folder", "แฟ้มที่หายไป" },
        { "The mirror", "กระจก" },
        { "The portrait changed", "ภาพเปลี่ยนไป" },
        { "Basement Door", "ประตูห้องใต้ดิน" },
        { "Bedside Note", "โน้ตข้างเตียง" },
        { "Bench", "ม้านั่ง" },
        { "Cabinet 'M'", "ตู้ 'M'" },
        { "Cabinet Tag", "ป้ายตู้" },
        { "Calendar", "ปฏิทิน" },
        { "Exit Door", "ประตูทางออก" },
        { "Faded Poster", "โปสเตอร์ซีด" },
        { "Filing Cabinets", "ตู้เก็บเอกสาร" },
        { "Hospital Bed", "เตียงผู้ป่วย" },
        { "Keypad", "แป้นรหัส" },
        { "Medicine Cabinet", "ตู้ยา" },
        { "Mirror", "กระจก" },
        { "Notice Board", "ป้ายประกาศ" },
        { "Old Key", "กุญแจเก่า" },
        { "Patient Register", "ทะเบียนผู้ป่วย" },
        { "Phone", "โทรศัพท์" },
        { "Portrait", "ภาพเหมือน" },
        { "Portrait (changed)", "ภาพเหมือน (เปลี่ยนไป)" },
        { "Reception Desk", "โต๊ะต้อนรับ" },
        { "Stain on the Floor", "คราบบนพื้น" },
        { "Torn Photo", "รูปถ่ายที่ฉีกขาด" },
        { "Wall Clock", "นาฬิกาแขวน" },
        { "Wall Writing", "ตัวหนังสือบนผนัง" },
        { "The door will not open. I feel like I'm missing something", "ประตูไม่เปิด เหมือนฉันยังขาดอะไรบางอย่าง" },
        { " That was your LAST hint. Ask again and you die.", " นี่คือคำใบ้ครั้งสุดท้าย ถ้าขออีกคุณตาย" },
        { "the calendar in the Waiting Hall", "ปฏิทินในห้องรอ" },
        { "the clock in the Ward", "นาฬิกาในหอผู้ป่วย" },
        { "the stain on the floor in the Records Room", "คราบบนพื้นในห้องเก็บบันทึก" },
        { "the writing on the wall in the Records Room", "ตัวหนังสือบนผนังในห้องเก็บบันทึก" },
        { "the faded poster at the far left of the Waiting Hall", "โปสเตอร์ซีดทางซ้ายสุดของห้องรอ" },
        { "the note on the wall near the hospital bed in the Ward", "โน้ตบนผนังใกล้เตียงในหอผู้ป่วย" },
        { "the tag between the filing cabinets in the Records Room", "ป้ายระหว่างตู้เก็บเอกสารในห้องเก็บบันทึก" },
        { "A tag hangs between the cabinets:", "ป้ายแขวนอยู่ระหว่างตู้:" },
        { "A crumpled note stuck to the wall:", "กระดาษยับๆ แปะอยู่บนผนัง:" },
        { "A faded poster. Someone wrote numbers in the margin:", "โปสเตอร์ซีดๆ มีคนเขียนตัวเลขไว้ที่ขอบ:" },
        { "Scratched into the wall, over and over:", "ถูกขูดซ้ำๆ ลงบนผนัง:" },
        { "The clock stopped long ago. Something is written on its glass:", "นาฬิกาหยุดเดินมานานแล้ว มีบางอย่างเขียนอยู่บนกระจก:" },
        { "A calendar. Some dates are circled in red:", "ปฏิทิน บางวันที่ถูกวงสีแดงไว้:" },
        { "A dark stain. Someone dragged a finger through it:", "คราบสีเข้ม มีคนเอานิ้วลากผ่านมัน:" },
        { "Notes are pinned to the board. One of them has a number:", "มีโน้ตปักอยู่บนกระดาน โน้ตแผ่นหนึ่งมีตัวเลขเขียนอยู่:" },
        { "Just old, blank notes. Nothing useful here.", "มีแค่โน้ตเก่าๆ ที่ว่างเปล่า ไม่มีอะไรมีประโยชน์" },
        { "Notice board", "กระดานประกาศ" },
        { "the notice board", "กระดานประกาศ" },
        { "The door is locked. There is a keypad beside it.", "ประตูล็อกอยู่ มีแผงกดรหัสอยู่ข้างๆ" },
        { "Red folder", "แฟ้มสีแดง" },
        { "A red folder. The label is torn off.", "แฟ้มสีแดง ป้ายชื่อฉีกขาดไปแล้ว" },
        { "Picked up a red folder.", "เก็บแฟ้มสีแดงแล้ว" },
        { "Click anywhere to close", "คลิกที่ไหนก็ได้เพื่อปิด" },
        { "Numbers are scratched into the wall:", "ตัวเลขถูกขูดไว้บนผนัง:" },
        { "Just old scratches and dust. Nothing useful here.", "มีแค่รอยขีดข่วนเก่าๆ กับฝุ่น ไม่มีอะไรมีประโยชน์" },
        { "DOOR PASSCODE", "รหัสผ่านประตู" },
        { "DOOR PASSCODE\n? - ? - ? - ?", "รหัสผ่านประตู\n? - ? - ? - ?" },
        { "A photo of a girl. Someone tore away the other half.", "รูปถ่ายของเด็กผู้หญิง มีคนฉีกอีกครึ่งหนึ่งทิ้งไป" },
        { "A rusty old key. It looks like it fits a heavy door.", "กุญแจเก่าขึ้นสนิม ดูเหมือนจะใช้ไขประตูบานหนักได้" },
        { "The last entry is dated three years ago. One name is circled in red: 'Mali'.", "รายการสุดท้ายลงวันที่เมื่อสามปีก่อน มีชื่อหนึ่งถูกวงสีแดงไว้: 'มะลิ'" },
        { "A photo of the health station staff from years ago. Part of it has been torn away.", "ภาพถ่ายเจ้าหน้าที่สถานีอนามัยในอดีต ถูกฉีกออกบางส่วน" },
        { "A cabinet labelled 'M'. A torn document fragment is stuck in the drawer. A page of the case report is missing.", "ตู้เก็บเอกสารติดป้าย 'M' มีชิ้นส่วนเอกสารติดอยู่ในลิ้นชัก ข้อมูลบางส่วนในรายงานคดีขาดหายไป" },
        { "A tray of old medical instruments. Something abnormal once happened inside this health station.", "ถาดอุปกรณ์แพทย์เก่า บ่งบอกถึงเหตุการณ์ผิดปกติที่เคยเกิดขึ้นภายในสถานีอนามัย" },
        { "A board pinned with photos, scraps of paper and instruments. Put them together and analyse them.", "กระดานติดภาพถ่าย เศษเอกสาร และอุปกรณ์ ลองนำมาวิเคราะห์ร่วมกัน" },
        { "Old medical records. Some patients' names are crossed out, next to their treatment dates. One name is circled in red: 'Mali'.", "เวชระเบียนผู้ป่วยเก่า ชื่อผู้ป่วยบางคนถูกขีดฆ่า พร้อมวันที่รับการรักษา มีชื่อหนึ่งถูกวงสีแดงไว้: 'มะลิ'" },
        { "An old key. It opens a locked room.", "กุญแจเก่าๆ ใช้เปิดห้อง" },
        { "A direct line phone. A call came in from inside the building, yet the outside line looks cut.", "โทรศัพท์สายตรง มีสายเรียกเข้าจากภายในอาคาร ทั้งที่สายภายนอกดูเหมือนถูกตัด" },
        { "Picked up: Old Patient Records", "เก็บแล้ว: เวชระเบียนผู้ป่วยเก่า" },
        { "Old Patient Records", "เวชระเบียนผู้ป่วยเก่า" },
        { "Medical Equipment", "อุปกรณ์แพทย์" },
        { "Medical equipment", "อุปกรณ์แพทย์" },
        { "Evidence Board", "กระดานเอกสาร" },
        { "Evidence board", "กระดานเอกสาร" },
        { "Direct Line", "โทรศัพท์สายตรง" },
        { "Direct line", "โทรศัพท์สายตรง" },
        { "Document fragment", "ชิ้นส่วนเอกสาร" },
        { "To be continued...", "โปรดติดตามตอนต่อไป..." },
        { "...footsteps. Someone is coming. Look around!", "...เสียงฝีเท้า มีใครบางคนกำลังมา มองรอบๆ!" },
        { "Door A (Room 1 side)", "ประตู A (ฝั่งห้อง 1)" },
        { "Door A (Room 2 side)", "ประตู A (ฝั่งห้อง 2)" },
        { "Door B (Room 2 side)", "ประตู B (ฝั่งห้อง 2)" },
        { "Door B (Room 3 side)", "ประตู B (ฝั่งห้อง 3)" },
        { "Too far away...", "ไกลเกินไป..." },
        { "I can't use this yet... I need something first.", "ยังใช้ไม่ได้... ต้องมีอะไรบางอย่างก่อน" },
        { "I still don't understand enough to do this...", "ฉันยังไม่เข้าใจพอที่จะทำสิ่งนี้..." },
        // ---- Room 4: Dressing Room (wound-care puzzle) ----
        { "ROOM 4: DRESSING ROOM", "ห้อง 4: ห้องทำแผล" },
        { "DRESSING ROOM", "ห้องทำแผล" },
        { "DRESSING TABLE", "โต๊ะทำแผล" },
        { "DRAWER CABINET", "ตู้ลิ้นชัก" },
        { "ROOM 5: PATIENT ROOM", "ห้อง 5: ห้องผู้ป่วย" },
        { "PATIENT ROOM", "ห้องผู้ป่วย" },
        { "BEDSIDE DRAWER", "ลิ้นชักข้างเตียง" },
        { "The door to the patient room creaks open...", "ประตูห้องผู้ป่วยเปิดออกพร้อมเสียงเอี๊ยดอ๊าด..." },
        { "The dressing room door is locked. I need a key.", "ประตูห้องทำแผลล็อกอยู่ ต้องมีกุญแจ" },
        { "A patient bed. The sheets are torn, as if someone clawed at them.", "เตียงผู้ป่วย ผ้าปูที่นอนฉีกขาด เหมือนมีคนใช้เล็บข่วน" },
        { "A patient bed. The mattress is still warm, but nobody is here.", "เตียงผู้ป่วย ที่นอนยังอุ่นอยู่ แต่ไม่มีใครอยู่ตรงนี้" },
        { "A patient bed. A name tag at the foot has been scratched off.", "เตียงผู้ป่วย ป้ายชื่อที่ปลายเตียงถูกขูดออกไปแล้ว" },
        { "A small cabinet with a drawer.", "ตู้เล็กๆ มีลิ้นชัก" },
        { "Dressing Room Key", "กุญแจห้องทำแผล" },
        { "A small brass key with a paper tag: 'Dressing Room'.", "กุญแจทองเหลืองดอกเล็ก มีป้ายกระดาษเขียนว่า 'ห้องทำแผล'" },
        { "Cursed Red File", "แฟ้มต้องสาปสีแดง" },
        { "Code Document", "เอกสารบอกรหัส" },
        { "The door is locked. The keypad on the wall wants a 6-digit code.", "ประตูล็อกอยู่ แผงกดรหัสบนผนังต้องการรหัสหกหลัก" },
        { "The Radio Room Code", "รหัสห้องวิทยุ" },
        { "The moment I stepped into the radio room, the door locked behind me. Then the old radio crackled to life, and a voice slowly read out six numbers. That is the code that opens the room: 021604.",
          "ทันทีที่ก้าวเข้ามาในห้องวิทยุ ประตูก็ล็อกลงข้างหลังฉัน แต่แล้ววิทยุเก่าก็ส่งเสียงซ่าขึ้นมา เสียงหนึ่งค่อยๆ อ่านตัวเลขหกหลักออกมา นั่นคือรหัสที่ใช้ปลดล็อกห้องนี้: 021604" },
        { "A document found in the drawer. One digit of the passcode is written on it.", "เอกสารที่เจอในลิ้นชัก มีตัวเลขของรหัสผ่านเขียนอยู่หนึ่งหลัก" },
        { "A red file with a smeared handprint on the cover. It feels cold to hold. Inside, a name has been scratched out.", "แฟ้มสีแดงมีรอยฝ่ามือเลอะอยู่บนปก จับแล้วเย็นเยียบ ข้างในมีชื่อถูกขูดทิ้งไป" },
        { "The door to the dressing room creaks open...", "ประตูห้องทำแผลเปิดออกพร้อมเสียงเอี๊ยดอ๊าด..." },
        { "Wound care order", "คำสั่งการทำแผล" },
        { "Wound care order: 1 tray, 2 bandage, 3 tools (scissors), 4 medicine / disinfectant.", "คำสั่งการทำแผล: 1 ถาด 2 ผ้าพันแผล 3 อุปกรณ์ (กรรไกร) 4 ยา / น้ำยาฆ่าเชื้อ" },
        { "Treatment report", "รายงานการรักษา" },
        { "A document hidden in the drawer. The patient's name was never written on it, only a date: the day the clinic closed.", "เอกสารที่ซ่อนอยู่ในลิ้นชัก ไม่มีชื่อผู้ป่วย มีแต่วันที่ ซึ่งตรงกับวันที่คลินิกปิด" },
        { "Nothing useful here.", "ไม่มีอะไรมีประโยชน์ตรงนี้" },
        { "nothing useful here", "ไม่มีอะไรมีประโยชน์ตรงนี้" },
    };

    // ---------------- Thai font + button, created automatically ----------------
    private static TMP_FontAsset thaiFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        MakeThaiFont();
        if (Object.FindAnyObjectByType<Localizer>() == null) { GameObject g = new GameObject("Localizer"); Object.DontDestroyOnLoad(g); g.AddComponent<Localizer>(); }
        if (Object.FindAnyObjectByType<LanguageButtonMarker>() != null) return;
        BuildButton();
    }

    private static void MakeThaiFont()
    {
        if (thaiFont != null) return;

        // 1) The font shipped with the game: Assets/Resources/Fonts/Sarabun-Regular.ttf (works in builds too)
        try
        {
            Font src = Resources.Load<Font>("Fonts/Sarabun-Regular");
            if (src != null)
            {
                thaiFont = TMP_FontAsset.CreateFontAsset(src, 90, 9,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                if (thaiFont != null) Debug.Log("Loc: Thai font = Sarabun (from Resources)");
            }
        }
        catch (System.Exception e) { Debug.LogWarning("Loc: Sarabun failed: " + e.Message); }

        // 2) Otherwise a font installed on this computer (not available in builds on other PCs)
        string[] names = { "Leelawadee UI", "Tahoma", "Noto Sans Thai", "Sarabun", "Arial Unicode MS" };
        foreach (string n in names)
        {
            if (thaiFont != null) break;
            try
            {
                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(n, "Regular", 90);
                if (asset == null) asset = TMP_FontAsset.CreateFontAsset(n, "Normal", 90);
                if (asset == null) continue;
                asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                asset.isMultiAtlasTexturesEnabled = true;
                thaiFont = asset;
                Debug.Log("Loc: Thai font = " + n);
            }
            catch (System.Exception e) { Debug.LogWarning("Loc: font " + n + " failed: " + e.Message); }
        }
        if (thaiFont != null)
        {
            // Put every Thai letter / vowel / tone mark into the atlas now, so none is missing later
            var thai = new System.Text.StringBuilder();
            for (int c = 0x0E01; c <= 0x0E5B; c++) thai.Append((char)c);
            thaiFont.TryAddCharacters(thai.ToString(), out string notAdded);
            if (!string.IsNullOrEmpty(notAdded)) Debug.LogWarning("Loc: Thai characters not in font: " + notAdded.Length);
        }
        if (thaiFont == null) { Debug.LogWarning("Loc: no Thai font found on this computer."); return; }
        if (TMP_Settings.fallbackFontAssets == null) return;
        if (!TMP_Settings.fallbackFontAssets.Contains(thaiFont)) TMP_Settings.fallbackFontAssets.Add(thaiFont);
        if (TMP_Settings.defaultFontAsset != null && TMP_Settings.defaultFontAsset.fallbackFontAssetTable != null &&
            !TMP_Settings.defaultFontAsset.fallbackFontAssetTable.Contains(thaiFont))
            TMP_Settings.defaultFontAsset.fallbackFontAssetTable.Add(thaiFont);
    }

    private static void BuildButton()
    {
        GameObject root = new GameObject("Language Button");
        root.AddComponent<LanguageButtonMarker>();
        Object.DontDestroyOnLoad(root);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem (Language)");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
            Object.DontDestroyOnLoad(es);
        }

        GameObject b = new GameObject("Button");
        b.transform.SetParent(root.transform, false);
        RectTransform rt = b.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);   // bottom-left corner
        rt.anchoredPosition = new Vector2(20, 20);
        rt.sizeDelta = new Vector2(150, 50);
        Image img = b.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.65f);
        Button btn = b.AddComponent<Button>();

        GameObject t = new GameObject("Label");
        t.transform.SetParent(b.transform, false);
        RectTransform trt = t.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI label = t.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 26;
        label.color = Color.white;
        label.raycastTarget = false;
        if (thaiFont != null) label.font = thaiFont;

        System.Action refresh = () => label.text = Thai ? "ภาษา: ไทย" : "Lang: EN";
        refresh();
        btn.onClick.AddListener(() => { Set(!Thai); refresh(); });
        Changed += refresh;
    }
}

public class LanguageButtonMarker : MonoBehaviour { }

// Re-translates every text on screen (set by the scene or by scripts) while Thai is on
public class Localizer : MonoBehaviour
{
    private readonly Dictionary<TMP_Text, string> original = new Dictionary<TMP_Text, string>();
    private readonly Dictionary<TMP_Text, string> shown = new Dictionary<TMP_Text, string>();
    private float next;
    private bool lastThai;

    private void Update()
    {
        if (Time.unscaledTime < next && lastThai == Loc.Thai) return;
        next = Time.unscaledTime + 0.2f;
        lastThai = Loc.Thai;

        TMP_Text[] all = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text t in all)
        {
            if (t == null || !t.gameObject.scene.IsValid()) continue;
            string cur = t.text;
            if (string.IsNullOrEmpty(cur)) continue;

            // Did a script write new text since we last touched it?
            if (!shown.TryGetValue(t, out string mine) || mine != cur) original[t] = cur;

            string orig = original[t];
            string want = Loc.Thai ? Loc.Shape(Loc.T(orig)) : orig;
            if (want != cur)
            {
                t.text = want;
                shown[t] = want;
            }
            else shown[t] = cur;
        }
    }
}

