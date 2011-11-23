// binsearch.cpp : définit le point d'entrée pour l'application console.
//

#include "stdafx.h"

#include "ibbsearch.h"

#include "../bplist/bplist.h"


struct what
{
	const char *pattern;
	std::list<std::wstring>	found;

	void add(const std::wstring& message)
	{
		printf("%ls\n", message.c_str());
		found.push_back(message);
	}
};


struct position
{
	size_t	offset;
	size_t	line, column;
};


#pragma region utf8_reader class

class utf8_reader
{
	const unsigned char *ptr;	// the memory buffer
	size_t length;
	
	size_t current;				// current position in the buffer

	bool is_textfile;			// true if the buffer contains only printable characters
	size_t line, col;			// computed line and column of current position

public:
	utf8_reader(const void *ptr, size_t length)
	{
		this->ptr = static_cast<const unsigned char *>(ptr);
		this->length = length;

		reset();
	}


	// restart the reader
	inline void reset()
	{
		is_textfile = true;
		line = 1;
		col = 0;
		current = 0;
	}


	// returns the current position information
	inline position offset() const
	{
		position p;

		p.offset = current;
		
		if (is_textfile) {
			p.column = col;
			p.line = line;
		}
		else {
			p.column = 0;
			p.line = 0;
		}

		return p;
	}


	// true if the end has been reached
	inline bool eof() const
	{
		return current >= length;
	}


	//
	inline void skip(size_t n)
	{
		current += n;
		if (current > length) current = length;
	}


	// get the next byte
	inline bool next_byte(unsigned char& byte)
	{
		if (current >= length) return false;

		byte = ptr[current++];
		return true;
	}


	// get the next valid UTF-8 character
	bool next_utf8(wchar_t& c)
	{		
		// désactive provisoirement le flag
		// si on a lu un caractère valide et que le flag était à true
		// on le remettra à true
		bool textfile = is_textfile;
		is_textfile = false;			

		size_t curr = current;
		unsigned char b;

		if (! next_byte(b)) {
			return false;
		}

		if ((b & 0x80) == 0x00) {				// 0xxxxxxx
			c = b;
		}
		else if ((b & 0xC0) == 0x80) {			// 10xxxxxx

			// impossible de commencer un caractère UTF-8 comme ça
			return false;
		}
		else {
			int n;

			if ((b & 0xE0) == 0xC0) {			// 110xxxxx
				c = (b & 0x1F);					// 00011111
				n = 1;
			}
			else if ((b & 0xF0) == 0xE0) {		// 1110xxxx
				c = (b & 0x0F);					// 00001111
				n = 2;
			}
			else if ((b & 0xF8) == 0xF0) {		// 11110xxx
				c = (b & 0x07);					// 00000111
				n = 3;
			}
			else if ((b & 0xFC) == 0xF8) {		// 111110xx
				c = (b & 0x03);					// 00000011
				n = 4;
			}
			else if ((b & 0xFE) == 0xFC) {		// 1111110x
				c = (b & 0x01);					// 00000001
				n = 5;
			}
			else {
				current = curr + 1;
				return false;
			}

			while (n-- > 0) {

				if (! next_byte(b) || (b & 0xC0) != 0x80) {	// 10xxxxxx
					current = curr + 1;
					return false;
				}

				c = (c << 6) | (b & 0x3F);		// 00111111
			}
		}

		if (textfile) {
			is_textfile = true;
			if (c == L'\n') { 
				++line;
				col = 0; 
			}
			else {
				++col;
			}
		}

		return true;
	}
};

#pragma endregion


std::wstring printables(const void *ptr, size_t length, size_t start = 0, size_t max_length = 0)
{
	utf8_reader buf(ptr, length);

	std::wstring s;
	wchar_t c;
	
	buf.skip(start);
	
	while (! buf.eof()) {

		if (buf.next_utf8(c)) {

			if (iswspace(c))
				s += L" ";
			else if (iswprint(c))
				s.append(1, c);

			if (max_length != 0 && s.length() >= max_length)
				break;
		}
	}

	return s;
}



#pragma region search algorithm

void search_buffer(const void *ptr, size_t length, const char *pattern, std::vector<position>& offsets)
{
	utf8_reader buf(ptr, length);

	wchar_t c;					// le caractère lu
	size_t idx = 0;				// index dans pattern[]
	position start;				// position du début du pattern
	size_t ignore_punct = 0;	// compteur d'ignore des ponctuations
	size_t ignore_space = 0;	// compteur d'ignore des espaces

	while (! buf.eof()) {

		////////////////////////////////////////////////////////

		position current = buf.offset();

		if (! buf.next_utf8(c)) {
			// RESET
			idx = 0;
			continue;
		}
		
		wchar_t w[8];
		int n = NormalizeString(NormalizationD, &c, 1, w, 8);
		if (n == 0) {
			// RESET
			idx = 0;
			continue;
		}
		c = w[0];

		////////////////////////////////////////////////////////


		if (iswspace(c)) {

			// MAX IGNORE 3
			if (idx != 0) {
				ignore_space++;
				if (ignore_space > 3) {
					// RESET
					idx = 0;
					continue;
				}
			}

			// IGNORE
			continue;
		}
		ignore_space = 0;
		

		if (iswpunct(c)) {

			// MAX IGNORE 2
			if (idx != 0) {
				ignore_punct++;
				if (ignore_punct > 2) {
					// RESET
					idx = 0;
					continue;
				}
			}

			// IGNORE
			continue;
		}
		ignore_punct = 0;


		if (c > 0x007F) {
			// RESET
			idx = 0;
			continue;
		}

		char b = (char)c;


		// UPPERCASE
		if (b >= 'a' && b <= 'z')
			b -= 'a' - 'A';
		
		if (b == pattern[idx]) {

			if (idx == 0)
				start = current;

			// CONTINUE
			idx++;

			// FOUND
			if (pattern[idx] == 0) {
				offsets.push_back(start);

				idx = 0;
			}
		}
		else {
			// RESET
			idx = 0;
			continue;
		}
	}
}

#pragma endregion



void search_mem_buffer(const void *ptr, size_t len, what *what, const wchar_t *info)
{
	std::vector<position> offsets;

	search_buffer(ptr, len,  what->pattern, offsets);

	if (offsets.empty()) 
		return;
	
	std::wstringstream m;

	if (info)
		m << info;
	else {
		m << L"mem";
	}

	if (offsets.size() == 1) {
		m << L" at offset " << offsets[0].offset;
	}
	else {
		m << L" at offsets ";
		for (size_t n = 0; n < offsets.size(); ++n) {
			if (n != 0) m << L",";
			m << offsets[n].offset;
		}
	}

	size_t o = offsets[0].offset;
	o = (o > 10) ? o - 10 : 0;

	m << L'\x1B' << printables(ptr, len, o, 10 + strlen(what->pattern) + 10) << L'\x1B';

	what->add(m.str());	
}



#pragma region SQLite search

struct callback_params
{
	sqlite3 *db;
	what *what;
};

static int callback(void *ptr, int argc, char **argv, char **azColName)
{
	callback_params *self = static_cast<callback_params *>(ptr);

	if (argc != 1)
		return 0;

	//printf("TABLE %s\n", argv[0]);

	std::string utf8_cmd;

	utf8_cmd = "select _ROWID_,* from ";
	utf8_cmd += argv[0];
	utf8_cmd += "";

	sqlite3_stmt *stmt;

	sqlite3_prepare_v2(self->db, utf8_cmd.c_str(), -1, &stmt, NULL);
	
	/*
	int cols = sqlite3_column_count(stmt);
	for (int i = 0; i < cols; ++i) {
		printf("  col %2d: %-30ls %ls\n", 
			i, 
			sqlite3_column_name16(stmt, i),
			sqlite3_column_decltype16(stmt, i));
	}
	*/

	size_t row = 0;

	while (sqlite3_step(stmt) == SQLITE_ROW) {

		++row;

		int count = sqlite3_data_count(stmt);

		if (count == 0)
			continue;

		sqlite3_int64 rowid = sqlite3_column_int64(stmt, 0);

		for (int i = 1; i <  count; ++i) {

			const void *blob = sqlite3_column_blob(stmt, i);
			int length = sqlite3_column_bytes(stmt, i);

			std::vector<position> offsets;

			
			search_buffer(blob, length, self->what->pattern, offsets);

			if (! offsets.empty()) {

				//fwrite(blob, 1, length, stdout);
				//puts("");

				std::wstringstream m;

				/*
				m << L"table '" << argv[0]
				  << L"' column '" << (const wchar_t *) sqlite3_column_name16(stmt, i)
				  << L"' rowid " << rowid;
				*/
				m << L"in field " << argv[0] << L"." << (const wchar_t *) sqlite3_column_name16(stmt, i)
				  << L" rowid " << rowid;

				size_t o = offsets[0].offset;
				o = (o > 10) ? o - 10 : 0;

				m << L'\x1B' << printables(blob, length, o, 10 + strlen(self->what->pattern) + 10) << L'\x1B';

				self->what->add(m.str());
			}
		}
	}

	sqlite3_finalize(stmt);

	return 0;
}


bool search_sqlite(const wchar_t *filename, what *what)
{
	sqlite3 *db = NULL;
	int rc, n;
	std::string utf8_filename;

	utf8_filename.resize(wcslen(filename) * 6);

	n = WideCharToMultiByte(CP_UTF8, 0, filename, -1, 
		const_cast<char *>(utf8_filename.data()),
		(int) utf8_filename.length(), NULL, NULL);

	utf8_filename.resize(n);

	rc = sqlite3_open_v2(utf8_filename.c_str(), &db, SQLITE_OPEN_READONLY, NULL);
	if (rc != SQLITE_OK) {
		fprintf(stderr, "Can't open database: %s\n", sqlite3_errmsg(db));
		sqlite3_close(db);
		return false;
	}

	char *zErrMsg = NULL;
	
	callback_params self;
	self.db = db;
	self.what = what;

	rc = sqlite3_exec(db, "select name from sqlite_master where type='table'", &callback, static_cast<callback_params *>(&self), &zErrMsg);
	if (rc != SQLITE_OK) {
		fprintf(stderr, "SQL error: %s\n", zErrMsg);
		sqlite3_free(zErrMsg);
	}

	sqlite3_close(db);
	return true;
}

#pragma endregion


bool search_mem_file(const wchar_t *filename, what *what)
{
	HANDLE hFile;
	HANDLE hMap;
	LARGE_INTEGER fileSize;
	const void *ptr;

	//hFile = (HANDLE) _get_osfhandle(_fileno(f));

	hFile = CreateFile(filename, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
	if (hFile == INVALID_HANDLE_VALUE) 
		return false;

	GetFileSizeEx(hFile, &fileSize);
	if (fileSize.HighPart != 0) {
		CloseHandle(hFile);
		return false;
	}

	hMap = CreateFileMapping(hFile, NULL, PAGE_READONLY, fileSize.HighPart, fileSize.LowPart, NULL);
	if (hMap != NULL) {
		ptr = MapViewOfFile(hMap, FILE_MAP_READ, 0, 0, fileSize.LowPart);

		search_mem_buffer(ptr, fileSize.LowPart, what, NULL);

		UnmapViewOfFile(ptr);

		CloseHandle(hMap);
	}
			
	CloseHandle(hFile);

	return true;
}


bool search_file(const wchar_t *filename, what *what)
{
	FILE *f;
	char buf[16];
	size_t len;

	if (_wfopen_s(&f, filename, L"rb") != 0)
		return false;

	// first 16 bytes are enough to determine the type of the file
	len = fread(buf, 1, 16, f);
	fclose(f);

	if (len != 16) {
		search_mem_buffer(buf, len, what, NULL);
	}

	else if (memcmp(buf, "SQLite format 3\0", 16) == 0) {
		search_sqlite(filename, what);
	}

	else if (memcmp(buf, "bplist00", 8) == 0) {
	
		char *xml = NULL;
		int len;
		
		// use BPLIST.DLL decoder
		len = bplist2xml_file2(filename, &xml, 1);

		if (xml) {
			search_mem_buffer(xml, len, what, L"xml");
			bplist2xml_free(xml);
		}
		else {
			search_mem_file(filename, what);
		}
	}

	else {

		// generic file
		search_mem_file(filename, what);
	}	

	return true;
}


#ifdef _CONSOLE

int wmain(int argc, wchar_t* argv[])
{
	//HMODULE hDll = LoadLibrary(L"normaliz.dll");
	//(FARPROC&) _NormalizeString = GetProcAddress(hDll, "NormalizeString");

	//if (_NormalizeString == NULL) return 2;

	what w;
	w.pattern = "TEST";
	search_file(L"wifi.plist", &w);

	w.pattern = "TEST";
	search_file(L"sms.db", &w);
	return 0;
}

#endif


#ifdef _USRDLL

static void copy(wchar_t **& out, const std::wstring& in)
{
	if (out) {
		*out = (wchar_t *) CoTaskMemAlloc(sizeof(wchar_t) * (in.length() + 1));	
		wcscpy_s(*out, in.length() + 1, in.c_str());
	}
}


IBBSEARCH_API
void __stdcall search(const wchar_t *filename, const wchar_t *pattern, wchar_t **results)
{
	std::string s;

	s.resize(wcslen(pattern));
	for (size_t i = 0; pattern[i]; ++i)
		s[i] = (char) pattern[i];

	what w;
	w.pattern = s.c_str();
	
	search_file(filename, &w);

	std::wstring list;
	std::list<std::wstring>::const_iterator it;

	for (it = w.found.begin(); it != w.found.end(); ++it) {
		list += *it;
		list += L"\r\n";
	}
	
	copy(results, list);
}

#endif
