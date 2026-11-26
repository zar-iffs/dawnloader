import http.server, os, time, sys
R='/tmp/mirror'
class H(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        f=os.path.join(R, os.path.basename(self.path))
        if not os.path.isfile(f): self.send_response(404); self.end_headers(); return
        d=open(f,'rb').read(); self.send_response(200); self.send_header('Content-Length',str(len(d))); self.end_headers()
        for i in range(0,len(d),1<<20):
            self.wfile.write(d[i:i+(1<<20)]); time.sleep(0.15)
    def log_message(self,*a): sys.stderr.write(self.path+"\n")
http.server.ThreadingHTTPServer(('127.0.0.1',8770),H).serve_forever()
