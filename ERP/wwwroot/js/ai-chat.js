/**
 * SmartERP AI Business Intelligence Chatbot Client Script
 * Secure, Theme-Aware, Chart.js Integrated, Speech Recognition Supported
 */

(function () {
    'use strict';

    let conversationId = sessionStorage.getItem('erp_ai_conv_id') || ('conv_' + Math.random().toString(36).substr(2, 9));
    sessionStorage.setItem('erp_ai_conv_id', conversationId);

    let activeCharts = {};
    let speechRecognition = null;
    let isListening = false;

    // Elements cache
    const elements = {
        launcherBtn: document.getElementById('aiChatLauncherBtn'),
        launcherPill: document.getElementById('aiChatLauncherPill'),
        chatPanel: document.getElementById('aiChatPanel'),
        chatBody: document.getElementById('aiChatBody'),
        chatInput: document.getElementById('aiChatInput'),
        sendBtn: document.getElementById('aiChatSendBtn'),
        voiceBtn: document.getElementById('aiChatVoiceBtn'),
        closeBtn: document.getElementById('aiChatCloseBtn'),
        expandBtn: document.getElementById('aiChatExpandBtn'),
        clearBtn: document.getElementById('aiChatClearBtn'),
        tokenInput: document.querySelector('input[name="__RequestVerificationToken"]')
    };

    // Initialize Widget
    function init() {
        if (!elements.chatPanel) return;

        // Event Listeners
        if (elements.launcherBtn) {
            elements.launcherBtn.addEventListener('click', toggleChatPanel);
        }
        if (elements.launcherPill) {
            elements.launcherPill.addEventListener('click', openChatPanel);
        }
        if (elements.closeBtn) {
            elements.closeBtn.addEventListener('click', closeChatPanel);
        }
        if (elements.expandBtn) {
            elements.expandBtn.addEventListener('click', toggleFullscreen);
        }
        if (elements.clearBtn) {
            elements.clearBtn.addEventListener('click', clearChatHistory);
        }
        if (elements.sendBtn) {
            elements.sendBtn.addEventListener('click', handleSendMessage);
        }
        if (elements.chatInput) {
            elements.chatInput.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    handleSendMessage();
                }
            });
        }

        // Initialize Speech Recognition if supported
        initSpeechRecognition();

        // Delegate click for suggestion chips
        document.addEventListener('click', function (e) {
            const chip = e.target.closest('.ai-chip, .ai-suggest-chip');
            if (chip && chip.dataset.query) {
                openChatPanel();
                if (elements.chatInput) {
                    elements.chatInput.value = chip.dataset.query;
                    handleSendMessage();
                }
            }
        });

        // Keyboard Shortcut: Alt + C to toggle chatbot
        document.addEventListener('keydown', function (e) {
            if (e.altKey && (e.key === 'c' || e.key === 'C')) {
                e.preventDefault();
                toggleChatPanel();
            }
        });

        // Global function for page triggers
        window.openERPBotWithPrompt = function (promptText) {
            openChatPanel();
            if (elements.chatInput) {
                elements.chatInput.value = promptText;
                handleSendMessage();
            }
        };
    }

    function toggleChatPanel() {
        if (!elements.chatPanel) return;
        if (elements.chatPanel.classList.contains('hidden')) {
            openChatPanel();
        } else {
            closeChatPanel();
        }
    }

    function openChatPanel() {
        if (!elements.chatPanel) return;
        elements.chatPanel.classList.remove('hidden');
        if (elements.launcherPill) elements.launcherPill.style.display = 'none';
        if (elements.chatInput) elements.chatInput.focus();
        scrollToBottom();
    }

    function closeChatPanel() {
        if (!elements.chatPanel) return;
        elements.chatPanel.classList.add('hidden');
        if (elements.launcherPill) elements.launcherPill.style.display = 'flex';
    }

    function toggleFullscreen() {
        if (!elements.chatPanel) return;
        elements.chatPanel.classList.toggle('fullscreen');
        const icon = elements.expandBtn.querySelector('i');
        if (icon) {
            if (elements.chatPanel.classList.contains('fullscreen')) {
                icon.className = 'fa-solid fa-compress';
            } else {
                icon.className = 'fa-solid fa-expand';
            }
        }
    }

    function scrollToBottom() {
        if (elements.chatBody) {
            setTimeout(() => {
                elements.chatBody.scrollTop = elements.chatBody.scrollHeight;
            }, 50);
        }
    }

    // Speech Recognition Setup
    function initSpeechRecognition() {
        if (!elements.voiceBtn) return;

        const SpeechRec = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SpeechRec) {
            elements.voiceBtn.style.display = 'none';
            return;
        }

        speechRecognition = new SpeechRec();
        speechRecognition.continuous = false;
        speechRecognition.interimResults = false;
        speechRecognition.lang = 'en-US';

        speechRecognition.onstart = function () {
            isListening = true;
            elements.voiceBtn.classList.add('listening');
            elements.voiceBtn.title = 'Listening... Speak now';
        };

        speechRecognition.onresult = function (event) {
            const transcript = event.results[0][0].transcript;
            if (elements.chatInput) {
                elements.chatInput.value = transcript;
                handleSendMessage();
            }
        };

        speechRecognition.onerror = function (event) {
            console.warn('Speech recognition error:', event.error);
            stopListening();
        };

        speechRecognition.onend = function () {
            stopListening();
        };

        elements.voiceBtn.addEventListener('click', function () {
            if (isListening) {
                speechRecognition.stop();
            } else {
                try {
                    speechRecognition.start();
                } catch (e) {
                    console.error('Speech error', e);
                }
            }
        });
    }

    function stopListening() {
        isListening = false;
        if (elements.voiceBtn) {
            elements.voiceBtn.classList.remove('listening');
            elements.voiceBtn.title = 'Speak your question';
        }
    }

    // Send Message Handler
    async function handleSendMessage() {
        if (!elements.chatInput || !elements.sendBtn) return;
        const text = elements.chatInput.value.trim();
        if (!text) return;

        // Append User Message to UI
        appendMessage('user', text);
        elements.chatInput.value = '';
        elements.sendBtn.disabled = true;

        // Show typing indicator
        const typingId = showTypingIndicator();
        scrollToBottom();

        try {
            const token = elements.tokenInput ? elements.tokenInput.value : '';
            const response = await fetch('/api/aichat/message', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    message: text,
                    conversationId: conversationId
                })
            });

            removeTypingIndicator(typingId);

            if (!response.ok) {
                const errText = await response.text();
                appendErrorMessage('Unable to process question. Please try again.');
                return;
            }

            const data = await response.json();
            if (data.success) {
                appendAssistantMessage(data);
            } else {
                appendErrorMessage(data.errorMessage || data.message || 'An error occurred.');
            }
        } catch (err) {
            console.error('AIChat fetch error:', err);
            removeTypingIndicator(typingId);
            appendErrorMessage('Network timeout or connection error. Please verify your connection.');
        } finally {
            elements.sendBtn.disabled = false;
            scrollToBottom();
        }
    }

    // Clear Chat History
    async function clearChatHistory() {
        if (confirm('Clear the current conversation history?')) {
            try {
                const token = elements.tokenInput ? elements.tokenInput.value : '';
                await fetch('/api/aichat/clear', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify({ conversationId: conversationId })
                });

                // Generate new conversation id
                conversationId = 'conv_' + Math.random().toString(36).substr(2, 9);
                sessionStorage.setItem('erp_ai_conv_id', conversationId);

                // Reset UI to welcome message
                if (elements.chatBody) {
                    elements.chatBody.innerHTML = `
                        <div class="ai-message assistant">
                            <div class="ai-message-avatar"><i class="fa-solid fa-robot"></i></div>
                            <div class="ai-message-content">
                                <p class="m-0">Hello! I am your <strong>SmartERP Business Intelligence Assistant</strong>. Ask me any question in simple English about your company's actual sales, purchases, inventory, or outstanding dues.</p>
                                <div class="ai-followup-chips mt-3">
                                    <button class="ai-chip" data-query="What is my total sales this month?"><i class="fa-solid fa-cart-shopping"></i> Total Sales This Month</button>
                                    <button class="ai-chip" data-query="Show my top 10 selling products"><i class="fa-solid fa-fire"></i> Top 10 Selling Products</button>
                                    <button class="ai-chip" data-query="Which products are out of stock?"><i class="fa-solid fa-triangle-exclamation"></i> Out-of-Stock Products</button>
                                    <button class="ai-chip" data-query="Show customer outstanding balances"><i class="fa-solid fa-hand-holding-dollar"></i> Customer Dues</button>
                                </div>
                            </div>
                        </div>
                    `;
                }
            } catch (e) {
                console.error(e);
            }
        }
    }

    // UI Renderers
    function escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function formatMarkdown(text) {
        if (!text) return '';
        let escaped = escapeHtml(text);

        // Bold: **text**
        escaped = escaped.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');

        // Italic: *text*
        escaped = escaped.replace(/\*(.*?)\*/g, '<em>$1</em>');

        // Line breaks
        escaped = escaped.replace(/\n\n/g, '<br><br>').replace(/\n/g, '<br>');

        return escaped;
    }

    function appendMessage(role, text) {
        if (!elements.chatBody) return;
        const time = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        const div = document.createElement('div');
        div.className = `ai-message ${role}`;
        div.innerHTML = `
            <div class="ai-message-avatar"><i class="fa-solid fa-user"></i></div>
            <div class="ai-message-content">
                <div>${escapeHtml(text)}</div>
                <div class="ai-message-time">${time}</div>
            </div>
        `;
        elements.chatBody.appendChild(div);
        scrollToBottom();
    }

    function appendAssistantMessage(data) {
        if (!elements.chatBody) return;
        const time = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        const div = document.createElement('div');
        div.className = 'ai-message assistant';

        let html = `
            <div class="ai-message-avatar"><i class="fa-solid fa-robot"></i></div>
            <div class="ai-message-content">
        `;

        // Period Badge
        if (data.reportingPeriod) {
            html += `<div class="ai-period-tag"><i class="fa-regular fa-calendar"></i> ${escapeHtml(data.reportingPeriod)}</div>`;
        }

        // Natural Language Text
        html += `<div>${formatMarkdown(data.message || data.naturalLanguageAnswer)}</div>`;

        // Summary KPI Cards
        if (data.summaryCards && data.summaryCards.length > 0) {
            html += '<div class="ai-summary-grid">';
            data.summaryCards.forEach(card => {
                const color = escapeHtml(card.colorClass || 'primary');
                html += `
                    <div class="ai-kpi-card ${color}">
                        <div class="ai-kpi-title">${escapeHtml(card.title)}</div>
                        <div class="ai-kpi-value">${escapeHtml(card.value)}</div>
                        ${card.subtitle ? `<div class="ai-kpi-subtitle">${escapeHtml(card.subtitle)}</div>` : ''}
                    </div>
                `;
            });
            html += '</div>';
        }

        // Data Table
        if (data.tableData && data.tableData.rows && data.tableData.rows.length > 0) {
            const t = data.tableData;
            html += `
                <div class="ai-table-container">
                    <table class="ai-table">
                        <thead>
                            <tr>
                                ${t.headers.map(h => `<th>${escapeHtml(h)}</th>`).join('')}
                            </tr>
                        </thead>
                        <tbody>
                            ${t.rows.map(row => `
                                <tr>
                                    ${row.map(cell => `<td>${escapeHtml(cell)}</td>`).join('')}
                                </tr>
                            `).join('')}
                        </tbody>
                        ${t.footerTotals && t.footerTotals.length > 0 ? `
                            <tfoot>
                                <tr>
                                    ${t.footerTotals.map(f => `<td>${escapeHtml(f)}</td>`).join('')}
                                </tr>
                            </tfoot>
                        ` : ''}
                    </table>
                </div>
            `;
        }

        // Chart Container
        let chartId = null;
        if (data.chartData && data.chartData.labels && data.chartData.labels.length > 0) {
            chartId = 'aiChart_' + Math.random().toString(36).substr(2, 9);
            html += `
                <div class="ai-chart-container">
                    <canvas id="${chartId}"></canvas>
                </div>
            `;
        }

        // Follow-Up Chips
        if (data.suggestedFollowUps && data.suggestedFollowUps.length > 0) {
            html += '<div class="ai-followup-chips">';
            data.suggestedFollowUps.forEach(f => {
                html += `<button class="ai-chip" data-query="${escapeHtml(f)}"><i class="fa-regular fa-comments"></i> ${escapeHtml(f)}</button>`;
            });
            html += '</div>';
        }

        html += `
                <div class="ai-message-time">${time} · ${escapeHtml(data.providerUsed || 'SmartERP AI')}</div>
            </div>
        `;

        div.innerHTML = html;
        elements.chatBody.appendChild(div);

        // Instantiate Chart if needed
        if (chartId && window.Chart && data.chartData) {
            renderChart(chartId, data.chartData);
        }

        scrollToBottom();
    }

    function renderChart(canvasId, config) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (activeCharts[canvasId]) {
            activeCharts[canvasId].destroy();
        }

        const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
        const textColor = isDark ? '#cbd5e1' : '#475569';
        const gridColor = isDark ? 'rgba(255, 255, 255, 0.08)' : 'rgba(0, 0, 0, 0.06)';

        const datasets = (config.datasets || []).map(ds => ({
            label: ds.label || 'Value',
            data: ds.data || [],
            backgroundColor: ds.backgroundColors || ds.backgroundColor || '#4f46e5',
            borderColor: ds.borderColor || '#4f46e5',
            borderWidth: 1.5,
            tension: 0.35,
            fill: config.chartType === 'line' ? false : true
        }));

        const chart = new Chart(canvas, {
            type: config.chartType || 'bar',
            data: {
                labels: config.labels || [],
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: !!config.title,
                        text: config.title,
                        color: textColor,
                        font: { size: 12, weight: 'bold' }
                    },
                    legend: {
                        display: config.chartType === 'doughnut' || config.chartType === 'pie' || datasets.length > 1,
                        labels: { color: textColor, boxWidth: 12, font: { size: 10.5 } }
                    }
                },
                scales: config.chartType === 'doughnut' || config.chartType === 'pie' ? {} : {
                    x: {
                        ticks: { color: textColor, font: { size: 10 } },
                        grid: { display: false }
                    },
                    y: {
                        ticks: { color: textColor, font: { size: 10 } },
                        grid: { color: gridColor }
                    }
                }
            }
        });

        activeCharts[canvasId] = chart;
    }

    function appendErrorMessage(errorText) {
        if (!elements.chatBody) return;
        const div = document.createElement('div');
        div.className = 'ai-message assistant';
        div.innerHTML = `
            <div class="ai-message-avatar" style="background:#ef4444;"><i class="fa-solid fa-triangle-exclamation"></i></div>
            <div class="ai-message-content" style="border-left: 3px solid #ef4444;">
                <div class="text-danger fw-semibold">${escapeHtml(errorText)}</div>
                <div class="mt-2">
                    <button class="btn btn-sm btn-outline-danger" onclick="window.retryLastQuestion()" style="font-size:11px; padding:2px 8px;">
                        <i class="fa-solid fa-rotate-right me-1"></i> Retry Question
                    </button>
                </div>
            </div>
        `;
        elements.chatBody.appendChild(div);
        scrollToBottom();
    }

    let lastSentQuestion = '';
    window.retryLastQuestion = function () {
        if (lastSentQuestion && elements.chatInput) {
            elements.chatInput.value = lastSentQuestion;
            handleSendMessage();
        }
    };

    function showTypingIndicator() {
        if (!elements.chatBody) return null;
        const id = 'typing_' + Date.now();
        const div = document.createElement('div');
        div.id = id;
        div.className = 'ai-message assistant';
        div.innerHTML = `
            <div class="ai-message-avatar"><i class="fa-solid fa-robot"></i></div>
            <div class="ai-message-content">
                <div class="ai-typing-indicator">
                    <div class="ai-typing-dot"></div>
                    <div class="ai-typing-dot"></div>
                    <div class="ai-typing-dot"></div>
                </div>
            </div>
        `;
        elements.chatBody.appendChild(div);
        return id;
    }

    function removeTypingIndicator(id) {
        if (!id) return;
        const el = document.getElementById(id);
        if (el) el.remove();
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
