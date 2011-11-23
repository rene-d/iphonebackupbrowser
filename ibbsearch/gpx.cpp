#include "stdafx.h"
#include <ctime>
#include "ibbsearch.h"

#include "../bplist/bplist.h"


struct callback_params
{
	sqlite3 *db;
	FILE *f;
};

double x, y;

static int callback(void *ptr, int argc, char **argv, char **azColName)
{
	callback_params *self = static_cast<callback_params *>(ptr);

	//printf("%s %s %s\n", argv[0], argv[1], argv[2]);
	
	double lat, lon;

	lat = atof(argv[1]);
	lon = atof(argv[2]);

	if (lat < 1) return 0;
	
	
	if ((lat-x)*(lat-x) + (lon-y)*(lon-y) < 0.1)
		return 0;
		
	x = lat;
	y = lon;
	
	time_t t = (time_t) floor(atof(argv[0]) + 978307200);
	
	struct tm *tm;
	
	tm = gmtime(&t);
	
	// 2010-05-20T15:06:58Z
	char buf[100];
	strftime(buf, 100, "%Y-%m-%dT%H:%M:%SZ", tm);
	
	
	fprintf(self->f, 
		"      <trkpt lat=\"%s\" lon=\"%s\">"
        "<ele>0</ele>"
        "<time>%s</time>"
      	"</trkpt>\n",
      argv[1], argv[2], buf);
	

	return 0;
}


int main()
{
	sqlite3 *db = NULL;
	int rc, n;


	rc = sqlite3_open_v2("4096c9ec676f2847dc283405900e284a7c815836", &db, SQLITE_OPEN_READONLY, NULL);
	if (rc != SQLITE_OK) {
		fprintf(stderr, "Can't open database: %s\n", sqlite3_errmsg(db));
		sqlite3_close(db);
		return 2;
	}

	char *zErrMsg = NULL;
	
	
	
	callback_params self;
	self.db = db;
	self.f = fopen("wifi.gpx", "wt");
	
	fprintf(self.f,
"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
"<gpx xmlns=\"http://www.topografix.com/GPX/1/1\" xmlns:xalan=\"http://xml.apache.org/xalan\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" creator=\"MotionX Live\" version=\"1.1\">\n"
"  <trk>\n"
"    <name>WifiLocation</name>\n"
"    <desc>WifiLocation</desc>\n"
"    <trkseg>\n");
	
	rc = sqlite3_exec(db, "select Timestamp, Latitude, Longitude from WifiLocation order by Timestamp", &callback, static_cast<callback_params *>(&self), &zErrMsg);
	if (rc != SQLITE_OK) {
		fprintf(stderr, "SQL error: %s\n", zErrMsg);
		sqlite3_free(zErrMsg);
	}

	fprintf(self.f,
"    </trkseg>\n"
"  </trk>\n"
"</gpx>\n");

	fclose(self.f);




	self.db = db;
	self.f = fopen("cell.gpx", "wt");
	
	fprintf(self.f,
"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
"<gpx xmlns=\"http://www.topografix.com/GPX/1/1\" xmlns:xalan=\"http://xml.apache.org/xalan\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" creator=\"MotionX Live\" version=\"1.1\">\n"
"  <trk>\n"
"    <name>CellLocation</name>\n"
"    <desc>CellLocation</desc>\n"
"    <trkseg>\n");
	
	rc = sqlite3_exec(db, "select Timestamp, Latitude, Longitude from CellLocation order by Timestamp", &callback, static_cast<callback_params *>(&self), &zErrMsg);
	if (rc != SQLITE_OK) {
		fprintf(stderr, "SQL error: %s\n", zErrMsg);
		sqlite3_free(zErrMsg);
	}

	fprintf(self.f,
"    </trkseg>\n"
"  </trk>\n"
"</gpx>\n");

	fclose(self.f);


	sqlite3_close(db);
	return 0;
}
