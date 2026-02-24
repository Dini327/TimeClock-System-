מסמך אפיון מערכת: Time Clock System
נכתב ע"י: דינה ריינר
תאריך: 19/02/2026
עודכן: 24/02/2026 (החלפת Auto-Close ב-Manual Admin Close)

מטרת המערכת
מערכת נוכחות מאובטחת (High-Integrity), המבטיחה אמינות דיווח מוחלטת על ידי תלות במקור זמן חיצוני ניטרלי (Europe/Zurich). המערכת מונעת זיופי נוכחות, מטפלת במקרי קיצון של רשת, ומספקת למנהלים תמונת מצב אמינה בזמן אמת.

ארכיטקטורה וטכנולוגיות (Tech Stack)
Client-Side (Frontend):
•	Framework: React 19 (Vite) + TypeScript.
•	UI Library: Material UI (MUI) לעיצוב רספונסיבי ומקצועי.
•	State Management: React Query (ניהול מידע מהשרת ו-Caching).
•	Communication: Axios (HTTP Client).
Server-Side (Backend):
•	Framework: ASP.NET Core 9 Web API.
•	Architecture Pattern: N-Tier Architecture (שכבות):
Controller: קבלת בקשות HTTP ואימות קלט (Validation).
Service Layer: הליבה העסקית – ניהול הלוגיקה, בדיקת חוקיות דיווח, וקריאה ל-APIs חיצוניים.
Repository Layer: שכבת הגישה לנתונים (ביצוע פעולות מול ה-DB בלבד).
•	Resilience: שימוש בספריית Polly לניהול Retry Policies (ניסיונות חוזרים) ו-Circuit Breaker מול ספקי הזמן.
Database:
•	Microsoft SQL Server + Entity Framework Core (Code-First).

לוגיקה עסקית (Core Business Logic)
א. אסטרטגיית זמן ויתירות (Time Strategy & Failover):
המערכת תפעל במודל No-Trust Local Time. אין שימוש בשעון השרת לצרכי משכורת.
מקור ראשי: קריאה ל-API חיצוני: worldtimeapi.org (אזור Europe/Zurich).
מקור משני (Failover): במידה והראשון נכשל, קריאה ל-API גיבוי: timeapi.io.
כשל קריטי (System Alert):
o	במידה ושני המקורות נכשלים: המערכת תחסום את האפשרות לבצע Clock In/Out.
o	המערכת תיצור רשומה בטבלת SystemAlerts עם חומרת "Critical".
o	המשתמש יקבל הודעת שגיאה ברורה: "Service temporarily unavailable due to secure time verification failure".

ב. חוקיות דיווח (Validation Rules):
•	State Consistency: לא ניתן לבצע כניסה אם הסטטוס האחרון הוא "בפנים".
•	Orphan Shift Handling (Manual Admin Close):
o	משמרות פתוחות לא נסגרות אוטומטית בשום מצב.
o	אם עובד שכח לבצע יציאה ועברו יותר מ-12 שעות – המערכת תיצור התראת Warning עם ההודעה "User {Name} has an open shift for over 12 hours".
o	המשמרת תוצג באדום בלוח הבקרה של המנהל.
o	רק מנהל (Admin) יכול לסגור את המשמרת ידנית, על ידי ציון זמן סיום מדויק וסיבה מתועדת.

ג. סגירת משמרת ידנית על ידי מנהל (Manual Admin Close):
o	המנהל בוחר עובד עם משמרת פתוחה ולוחץ "Force Close".
o	נפתח מודאל המחייב:
     - ManualEndTime: DateTime picker לבחירת זמן סיום מדויק.
     - Reason: שדה טקסט חובה לתיעוד הסיבה.
o	בעת אישור, המערכת:
     1. שומרת רשומת ClockOut עם EventType = ManualClose.
     2. מגדירה IsManuallyClosed = true.
     3. שומרת את ה-ManualCloseReason לשרשרת הביקורת (Audit Trail).
     4. מגדירה TimeSource = "Admin-Override".
     5. יוצרת התראת Info: "Shift for {Name} was manually closed. Reason: …"

מבנה הנתונים (Data Model)
טבלת Users:
•	Id (PK), Email, PasswordHash, FullName, Role (Admin/Employee).

טבלת AttendanceLogs (לחישוב שכר):
•	Id (PK)
•	UserId (FK)
•	EventType (Enum: ClockIn, ClockOut, ManualClose)
•	OfficialTimestamp (DateTimeOffset - Zurich Time): הזמן הקובע לתשלום.
•	TimeSource (String): שם ה-API ממנו התקבל הזמן, או "Admin-Override" בסגירה ידנית.
•	IsManuallyClosed (Boolean): האם המשמרת נסגרה ידנית על ידי מנהל.
•	ManualCloseReason (String, Nullable): הסיבה שנמסרה על ידי המנהל בעת הסגירה.

טבלת SystemAlerts (לניהול תקלות וביקורת):
•	Id (PK)
•	AlertMessage (String): תיאור ההתראה.
•	Severity (Enum: Info, Warning, Critical).
•	CreatedAtUtc (DateTime - UTC): זמן יצירת ההתראה בשרת (לצרכי Audit/חקירה טכנית).

ממשק משתמש (UI)
Employee Dashboard:
•	כפתור חכם המציג סטטוס ("Clock In" / "Clock Out").
•	הצגת טיימר חי (Live Timer) המחושב בצד לקוח מרגע הכניסה האחרונה.
•	היסטוריית דיווחים אישית – כולל אינדיקציה "Admin Closed" למשמרות שנסגרו ידנית, והצגת הסיבה שנרשמה.

Admin Dashboard:
•	Live Status: רשימת עובדים פעילים כרגע.
•	הדגשה באדום: משמרות הפתוחות מעל 12 שעות מוצגות עם רקע אדום ואייקון אזהרה.
•	באנר אזהרה: אם קיימות משמרות אורפן, מוצגת הודעה בולטת בראש הדף.
•	Force-Close Modal: לחיצה על "Force Close" פותחת חלון מודאל הדורש:
o	DateTime picker לבחירת זמן הסיום המדויק.
o	שדה טקסט חובה לסיבת הסגירה.
•	Alerts Center (מרכז התראות):
o	אינדיקציה ויזואלית (פעמון אדום/הודעה בולטת) אם קיימות רשומות "Critical" מהשעה האחרונה.
o	התראות Warning מוצגות עבור משמרות אורפן (פתוחות מעל 12 שעות).
o	התראות Info מוצגות עבור כל סגירה ידנית ע"י מנהל, כולל הסיבה.
