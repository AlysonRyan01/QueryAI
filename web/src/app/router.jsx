import { Navigate } from 'react-router';
import AppLayout from '../components/Layout/AppLayout.jsx';
import ChangePasswordPage from '../pages/ChangePassword/ChangePasswordPage.jsx';
import ChatPage from '../pages/Chat/ChatPage.jsx';
import ConnectionsPage from '../pages/Connections/ConnectionsPage.jsx';
import HistoryPage from '../pages/History/HistoryPage.jsx';
import HomePage from '../pages/Home/HomePage.jsx';
import LoginPage from '../pages/Login/LoginPage.jsx';
import NotFoundPage from '../pages/NotFound/NotFoundPage.jsx';
import SettingsPage from '../pages/Settings/SettingsPage.jsx';
import UsersPage from '../pages/Settings/Users/UsersPage.jsx';
import { RequireAdmin, RequireAuth, RequirePasswordChanged } from './guards.jsx';

export const routes = [
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequireAuth />,
    children: [
      { path: '/trocar-senha', element: <ChangePasswordPage /> },
      {
        element: <RequirePasswordChanged />,
        children: [
          {
            element: <AppLayout />,
            children: [
              { path: '/', element: <HomePage /> },
              { path: '/conexoes', element: <ConnectionsPage /> },
              { path: '/chat', element: <ChatPage /> },
              { path: '/historico', element: <HistoryPage /> },
              {
                element: <RequireAdmin />,
                children: [
                  {
                    path: '/configuracoes',
                    element: <SettingsPage />,
                    children: [
                      { index: true, element: <Navigate to="usuarios" replace /> },
                      { path: 'usuarios', element: <UsersPage /> },
                    ],
                  },
                ],
              },
              { path: '*', element: <NotFoundPage /> },
            ],
          },
        ],
      },
    ],
  },
];
