-- Finestra semplice di emergenza (usata solo se l'interfaccia completa non parte)
on run argv
	set res to item 1 of argv
	set detail to ""
	if (count of argv) > 1 then set detail to item 2 of argv
	if (length of detail) > 220 then set detail to text 1 thru 220 of detail
	
	set iconSpec to 1
	try
		set iconSpec to (POSIX file (res & "/AppIcon.icns")) as alias
	end try
	
	set msg to "App in esecuzione" & return & return & "Inserisci nome cartella principale"
	if detail is not "" then set msg to msg & return & return & "(modalità semplice: " & detail & ")"
	
	set folderName to ""
	repeat while folderName is ""
		try
			set dlg to display dialog msg default answer "" buttons {"Esci", "Scatena"} default button "Scatena" cancel button "Esci" with title "WSiteFolderStart" with icon iconSpec
		on error number -128
			return
		end try
		set folderName to do shell script "printf '%s' " & quoted form of (text returned of dlg) & " | tr -d '/:' | sed 's/^[ .]*//;s/ *$//'"
	end repeat
	
	set rootPath to (POSIX path of (path to desktop folder)) & folderName
	do shell script "R=" & quoted form of rootPath & "; " & ¬
		"mkdir -p \"$R/Html\" \"$R/Css\" \"$R/Php\" \"$R/Js\" \"$R/Img\"; " & ¬
		"touch \"$R/Html/index.html\" \"$R/Css/style.css\" \"$R/Php/login.php\" \"$R/Js/script.js\""
end run
