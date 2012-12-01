package com.cobra.ldtp;
/*
LDTP v2 java client.

@author: Nagappan Alagappan <nagappan@gmail.com>
@copyright: Copyright (c) 2009-12 Nagappan Alagappan
@license: LGPL

http://ldtp.freedesktop.org

This file may be distributed and/or modified under the terms of the GNU Lesser General
Public License version 2 as published by the Free Software Foundation. This file
is distributed without any warranty; without even the implied warranty of 
merchantability or fitness for a particular purpose.

See 'COPYING' in the source distribution for more information.

Headers in this file shall remain intact.
*/

import java.util.Hashtable;
import java.util.Enumeration;
import java.lang.reflect.Method;
import java.util.concurrent.Executors;
import java.util.concurrent.ExecutorService;

public class PollLogs extends Thread {
    boolean pollServer = false;
    Ldtp ldtp = null;
    public PollLogs(Ldtp ldtp) {
	pollServer = true;
	this.ldtp = ldtp;
    }
    public void run() {
	String logMessage;
	String[] messageInfo;
	while(pollServer) {
	    try {
		logMessage = ldtp.getLastLog();
		if (logMessage.equals("")) {
		    // Sleep 1 second
		    Thread.sleep(1000);
		    continue;
		}
		/*
		  [0] - Message type
		  [1] - Message
		*/
		messageInfo = logMessage.split("-", 1);
		if (messageInfo[0].equals("MEMINFO"))
		    ldtp.log.
	    } catch(InterruptedException ex) {
		break;
	    }
	}
    }
}
