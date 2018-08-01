function sheetIDs = GetSheetIDs(DOCID, dataSheetsOnly)
% This function will get all sheet IDs from a given google spreadsheet, as
% specified with DOCID.  If dataSheetsOnly is set to true, then only data
% sheet IDs will be returned.  A datasheet is one in which the title is
% neither ALL CAPS or in brackets [].  I use such sheets to track metadata,
% and they do not contain individual mouse data.

% Inspired by GetGoogleSpreadsheet.m

api = 'AIzaSyBsb9u1iSEPKhotq3LSZxeAt-vAk9SrXsI';
loginURL = 'https://www.google.com'; 
dataURL = ['https://sheets.googleapis.com/v4/spreadsheets/' DOCID '?&fields=sheets.properties&key=' api];

%Step 1: go to google.com to collect some cookies
cookieManager = java.net.CookieManager([], java.net.CookiePolicy.ACCEPT_ALL);
java.net.CookieHandler.setDefault(cookieManager);
handler = sun.net.www.protocol.https.Handler;
connection = java.net.URL([],loginURL,handler).openConnection();
connection.getInputStream();

%Step 2: go to the spreadsheet export url and download the csv
connection2 = java.net.URL([],dataURL,handler).openConnection();
result = connection2.getInputStream();
result = char(readstream(result));

function out = readstream(inStream)
%READSTREAM Read all bytes from stream to uint8
%From: http://stackoverflow.com/a/1323535

import com.mathworks.mlwidgets.io.InterruptibleStreamCopier;
byteStream = java.io.ByteArrayOutputStream();
isc = InterruptibleStreamCopier.getInterruptibleStreamCopier();
isc.copyStream(inStream, byteStream);
inStream.close();
byteStream.close();
out = typecast(byteStream.toByteArray', 'uint8'); 

end

end