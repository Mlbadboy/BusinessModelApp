import React, { useEffect, useRef } from 'react';
import { Terminal } from 'xterm';
import 'xterm/css/xterm.css';

interface LiveTerminalProps {
    logs: string[];
}

const LiveTerminal: React.FC<LiveTerminalProps> = ({ logs }) => {
    const terminalRef = useRef<HTMLDivElement>(null);
    const xtermRef = useRef<Terminal | null>(null);

    useEffect(() => {
        if (!terminalRef.current) return;

        const term = new Terminal({
            cursorBlink: true,
            theme: {
                background: '#1e1e1e',
                foreground: '#ffffff',
            },
            fontFamily: 'Menlo, Monaco, "Courier New", monospace',
            fontSize: 14,
            rows: 20, // Set fixed rows since fit is disabled
            cols: 80  // Set fixed cols
        });

        term.open(terminalRef.current);
        xtermRef.current = term;

        return () => {
            term.dispose();
        };
    }, []);

    useEffect(() => {
        if (xtermRef.current && logs.length > 0) {
            const lastLog = logs[logs.length - 1];
            // Replace newlines with \r\n for xterm
            const formattedLog = lastLog.replace(/\n/g, '\r\n');
            xtermRef.current.writeln(formattedLog);
        }
    }, [logs]);

    return (
        <div
            ref={terminalRef}
            style={{ width: '100%', height: '400px', backgroundColor: '#1e1e1e', padding: '10px', borderRadius: '8px' }}
        />
    );
};

export default LiveTerminal;
