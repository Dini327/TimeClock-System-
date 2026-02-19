מסמך אפיון מערכת: Time Clock System
נכתב ע"י: דינה ריינר
תאריך: 19/02/2026
מטרת המערכת
מערכת נוכחות מאובטחת (High-Integrity), המבטיחה אמינות דיווח מוחלטת על ידי תלות במקור זמן חיצוני ניטרלי (Europe/Zurich). המערכת מונעת זיופי נוכחות, מטפלת במקרי קיצון של רשת, ומספקת למנהלים תמונת מצב אמינה בזמן אמת.
ארכיטקטורה וטכנולוגיות (Tech Stack)
Client-Side (Frontend):
•	Framework: React 18 (Vite) + TypeScript.
•	UI Library: Material UI (MUI) לעיצוב רספונסיבי ומקצועי.
•	State Management: React Query (ניהול מידע מהשרת ו-Caching).
•	Communication: Axios (HTTP Client).
Server-Side (Backend):
•	Framework: ASP.NET Core 8 Web API.
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
•	Orphan Shift Handling: אם עובד שכח לבצע יציאה ועברו יותר מ-16 שעות – המערכת תסגור את המשמרת אוטומטית (System Auto-Close) ותאפשר פתיחת משמרת חדשה.
מבנה הנתונים (Data Model)
טבלת Users:
•	Id (PK), Email, PasswordHash, FullName, Role (Admin/Employee).
טבלת AttendanceLogs (לחישוב שכר):
•	Id (PK)
•	UserId (FK)
•	EventType (Enum: ClockIn, ClockOut, AutoClose)
•	OfficialTimestamp (DateTimeOffset - Zurich Time): הזמן הקובע לתשלום.
•	TimeSource (String): שם ה-API ממנו התקבל הזמן (לצורכי בקרה).
טבלת SystemAlerts (לניהול תקלות):
•	Id (PK)
•	AlertMessage (String): תיאור התקלה (למשל: "All time APIs failed").
•	Severity (Enum: Info, Warning, Critical).
•	CreatedAtUtc (DateTime - UTC): זמן יצירת ההתראה בשרת (לצרכי Audit/חקירה טכנית).
ממשק משתמש (UI)
Employee Dashboard:
•	כפתור חכם המציג סטטוס ("Clock In" / "Clock Out").
•	הצגת טיימר חי (Live Timer) המחושב בצד לקוח מרגע הכניסה האחרונה.
•	היסטוריית דיווחים אישית.
Admin Dashboard:
•	Live Status: רשימת עובדים פעילים כרגע.
•	Alerts Center (מרכז התראות):
o	אינדיקציה ויזואלית (פעמון אדום/הודעה בולטת) אם קיימות רשומות "Critical" בטבלת SystemAlerts מהשעה האחרונה. מאפשר למנהל לדעת בזמן אמת אם יש בעיה מערכתית במקורות הזמן.
•	דוח שעות עם סימון חריגים (משמרות שנסגרו אוטומטית).