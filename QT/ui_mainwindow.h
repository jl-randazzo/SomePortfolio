/********************************************************************************
** Form generated from reading UI file 'mainwindow.ui'
**
** Created by: Qt User Interface Compiler version 5.13.2
**
** WARNING! All changes made in this file will be lost when recompiling UI file!
********************************************************************************/

#ifndef UI_MAINWINDOW_H
#define UI_MAINWINDOW_H

#include <QtCore/QVariant>
#include <QtWidgets/QAction>
#include <QtWidgets/QApplication>
#include <QtWidgets/QMainWindow>
#include <QtWidgets/QMenu>
#include <QtWidgets/QMenuBar>
#include <QtWidgets/QPushButton>
#include <QtWidgets/QStatusBar>
#include <QtWidgets/QWidget>
#include "framelistview.h"
#include "primaryframe.h"

QT_BEGIN_NAMESPACE

class Ui_MainWindow
{
public:
    QAction *actionOpen;
    QAction *actionImport_Sprites;
    QAction *actionUndo;
    QWidget *centralwidget;
    QWidget *primaryWidget;
    PrimaryFrame *primaryView;
    FrameListView *frameListView;
    QPushButton *genGrayScale;
    QPushButton *displayGray;
    QPushButton *displayLines;
    QPushButton *displayOriginal;
    QPushButton *displaySobel;
    QPushButton *displayErosion;
    QPushButton *displayDepop;
    QMenuBar *menubar;
    QMenu *menuFile;
    QMenu *menuEdit;
    QStatusBar *statusbar;

    void setupUi(QMainWindow *MainWindow)
    {
        if (MainWindow->objectName().isEmpty())
            MainWindow->setObjectName(QString::fromUtf8("MainWindow"));
        MainWindow->resize(1161, 651);
        actionOpen = new QAction(MainWindow);
        actionOpen->setObjectName(QString::fromUtf8("actionOpen"));
        actionImport_Sprites = new QAction(MainWindow);
        actionImport_Sprites->setObjectName(QString::fromUtf8("actionImport_Sprites"));
        actionUndo = new QAction(MainWindow);
        actionUndo->setObjectName(QString::fromUtf8("actionUndo"));
        centralwidget = new QWidget(MainWindow);
        centralwidget->setObjectName(QString::fromUtf8("centralwidget"));
        primaryWidget = new QWidget(centralwidget);
        primaryWidget->setObjectName(QString::fromUtf8("primaryWidget"));
        primaryWidget->setGeometry(QRect(0, 0, 1161, 631));
        primaryView = new PrimaryFrame(primaryWidget);
        primaryView->setObjectName(QString::fromUtf8("primaryView"));
        primaryView->setGeometry(QRect(80, 100, 1081, 511));
        frameListView = new FrameListView(primaryWidget);
        frameListView->setObjectName(QString::fromUtf8("frameListView"));
        frameListView->setGeometry(QRect(80, 0, 1081, 100));
        frameListView->setBaseSize(QSize(0, 0));
        frameListView->setStyleSheet(QString::fromUtf8("background-color: rgb(236, 236, 236);"));
        frameListView->setVerticalScrollBarPolicy(Qt::ScrollBarAlwaysOff);
        frameListView->setHorizontalScrollBarPolicy(Qt::ScrollBarAlwaysOff);
        frameListView->setFlow(QListView::LeftToRight);
        frameListView->setSelectionRectVisible(true);
        frameListView->setItemAlignment(Qt::AlignCenter|Qt::AlignHCenter|Qt::AlignJustify|Qt::AlignVCenter);
        genGrayScale = new QPushButton(primaryWidget);
        genGrayScale->setObjectName(QString::fromUtf8("genGrayScale"));
        genGrayScale->setGeometry(QRect(0, 100, 81, 61));
        displayGray = new QPushButton(primaryWidget);
        displayGray->setObjectName(QString::fromUtf8("displayGray"));
        displayGray->setGeometry(QRect(0, 220, 81, 31));
        displayLines = new QPushButton(primaryWidget);
        displayLines->setObjectName(QString::fromUtf8("displayLines"));
        displayLines->setGeometry(QRect(0, 250, 81, 31));
        displayOriginal = new QPushButton(primaryWidget);
        displayOriginal->setObjectName(QString::fromUtf8("displayOriginal"));
        displayOriginal->setGeometry(QRect(0, 190, 81, 31));
        displaySobel = new QPushButton(primaryWidget);
        displaySobel->setObjectName(QString::fromUtf8("displaySobel"));
        displaySobel->setGeometry(QRect(0, 310, 81, 31));
        displayErosion = new QPushButton(primaryWidget);
        displayErosion->setObjectName(QString::fromUtf8("displayErosion"));
        displayErosion->setGeometry(QRect(0, 280, 81, 31));
        displayDepop = new QPushButton(primaryWidget);
        displayDepop->setObjectName(QString::fromUtf8("displayDepop"));
        displayDepop->setGeometry(QRect(0, 340, 81, 31));
        MainWindow->setCentralWidget(centralwidget);
        menubar = new QMenuBar(MainWindow);
        menubar->setObjectName(QString::fromUtf8("menubar"));
        menubar->setGeometry(QRect(0, 0, 1161, 21));
        menuFile = new QMenu(menubar);
        menuFile->setObjectName(QString::fromUtf8("menuFile"));
        menuEdit = new QMenu(menubar);
        menuEdit->setObjectName(QString::fromUtf8("menuEdit"));
        MainWindow->setMenuBar(menubar);
        statusbar = new QStatusBar(MainWindow);
        statusbar->setObjectName(QString::fromUtf8("statusbar"));
        MainWindow->setStatusBar(statusbar);

        menubar->addAction(menuFile->menuAction());
        menubar->addAction(menuEdit->menuAction());
        menuFile->addAction(actionOpen);
        menuFile->addAction(actionImport_Sprites);
        menuEdit->addAction(actionUndo);

        retranslateUi(MainWindow);

        QMetaObject::connectSlotsByName(MainWindow);
    } // setupUi

    void retranslateUi(QMainWindow *MainWindow)
    {
        MainWindow->setWindowTitle(QCoreApplication::translate("MainWindow", "MainWindow", nullptr));
        actionOpen->setText(QCoreApplication::translate("MainWindow", "Open", nullptr));
        actionImport_Sprites->setText(QCoreApplication::translate("MainWindow", "Import", nullptr));
        actionUndo->setText(QCoreApplication::translate("MainWindow", "Undo", nullptr));
#if QT_CONFIG(shortcut)
        actionUndo->setShortcut(QCoreApplication::translate("MainWindow", "Ctrl+Z", nullptr));
#endif // QT_CONFIG(shortcut)
        genGrayScale->setText(QCoreApplication::translate("MainWindow", "Generate", nullptr));
        displayGray->setText(QCoreApplication::translate("MainWindow", "Show Gray", nullptr));
        displayLines->setText(QCoreApplication::translate("MainWindow", "Show Lines", nullptr));
        displayOriginal->setText(QCoreApplication::translate("MainWindow", "Show Orig", nullptr));
        displaySobel->setText(QCoreApplication::translate("MainWindow", "Show Sobel", nullptr));
        displayErosion->setText(QCoreApplication::translate("MainWindow", "Show Morph", nullptr));
        displayDepop->setText(QCoreApplication::translate("MainWindow", "Show Depop", nullptr));
        menuFile->setTitle(QCoreApplication::translate("MainWindow", "File", nullptr));
        menuEdit->setTitle(QCoreApplication::translate("MainWindow", "Edit", nullptr));
    } // retranslateUi

};

namespace Ui {
    class MainWindow: public Ui_MainWindow {};
} // namespace Ui

QT_END_NAMESPACE

#endif // UI_MAINWINDOW_H
