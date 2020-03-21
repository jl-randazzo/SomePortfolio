#ifndef QBASICS_H
#define QBASICS_H

#include<QImage>
#include<QString>
#include<QList>
#include<functional>
#include <QPainter>
#include <QDebug>

template<class T>
T findFirstInQList(QList<T>* list, std::function<bool(T)> search_function) {
    for(T item : *list){
        if(search_function(item))
            return item;
    }
    return nullptr;
}

#endif // QBASICS_H
